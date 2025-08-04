namespace Setback.DeepCfr.Model

open System.Collections

open PlayingCards
open Setback

/// Encoded value for input to a model.
type Encoding = BitArray

module Encoding =

    /// Converts encoded bits to float32.
    let toFloat32 (bits : BitArray) =
        [|
            for i = 0 to bits.Length - 1 do
                if bits[i] then 1f else 0f
        |]

    let encodedCardLength = Card.numCards

    /// Encodes the given (card, value) pairs as a vector
    /// in the deck size.
    let inline encodeCardValues pairs =
        let valueMap =
            pairs
                |> Seq.map (fun (card, value) ->
                    Card.toIndex card, value)
                |> Map
        let encoded =
            [|
                for index = 0 to Card.numCards - 1 do
                    valueMap
                        |> Map.tryFind index
                        |> Option.defaultValue
                            LanguagePrimitives.GenericZero   // encode to input type
            |]
        assert(encoded.Length = encodedCardLength)
        encoded

    /// Encodes the given cards as a multi-hot vector in
    /// the deck size.
    let encodeCards cards =
        let cardIndexes =
            cards
                |> Seq.map Card.toIndex
                |> set
        let encoded =
            [|
                for index = 0 to Card.numCards - 1 do
                    cardIndexes.Contains(index)
            |]
        assert(encoded.Length = encodedCardLength)
        encoded

    let encodedSeatLength = Seat.numSeats

    /// Encodes the given seat as a one-hot vector in the
    /// number of seats.
    let encodeSeat (seatOpt : Option<Seat>) =
        [|
            for seat in Seat.allSeats do
                Some seat = seatOpt
        |]

    let encodedPlayLength =
        encodedSeatLength + encodedCardLength

    let encodedCompleteTrickLength =
        Seat.numSeats * encodedPlayLength

    let encodedIncompleteTrickLength =
        (Seat.numSeats - 1) * encodedPlayLength

    /// Encodes each seat+card in the given trick as one-hot
    /// vectors and concatenates those vectors.
    let encodeTrick isCurrent trickOpt =
        let plays =
            trickOpt
                |> Option.map (Trick.plays >> Seq.toArray)
                |> Option.defaultValue Array.empty
        assert(
            match isCurrent, trickOpt.IsSome with
                | true, true -> plays.Length < Seat.numSeats
                | true, false -> false
                | false, true -> plays.Length = Seat.numSeats
                | false, false -> plays.Length = 0)
        let numPlays =
            if isCurrent then Seat.numSeats - 1
            else Seat.numSeats
        let encoded =
            [|
                for iPlay = 0 to numPlays - 1 do
                    let seatOpt, cards =
                        if iPlay < plays.Length then
                            let seat, card = plays[iPlay]
                            Some seat, [| card |]
                        else
                            None, Array.empty
                    yield! encodeSeat seatOpt
                    yield! encodeCards cards
            |]
        assert(encoded.Length =
            if isCurrent then encodedIncompleteTrickLength
            else encodedCompleteTrickLength)
        encoded

    let encodedPlayoutLength =
        (encodedCompleteTrickLength
            * (Setback.numCardsPerHand - 1))
            + encodedIncompleteTrickLength

    let encodePlayout playout =
        let pairs =
            [|
                match playout.CurrentTrickOpt with
                    | Some trick -> trick, true
                    | None -> failwith "No current trick"
                for trick in playout.CompletedTricks do
                    trick, false
            |]
        let encoded =
            [|
                for iTrick = Setback.numCardsPerHand - 1 downto 0 do
                    yield!
                        if iTrick < pairs.Length then
                            let trick, isCurrent = pairs[iTrick]
                            encodeTrick isCurrent (Some trick)
                        else
                            encodeTrick false None
            |]
        assert(encoded.Length = encodedPlayoutLength)
        encoded

    /// Total encoded length of an info set.
    let encodedLength =
        encodedCardLength            // current player's hand
            + encodedPlayoutLength   // playout

    /// Encodes the given info set as a vector.
    let encode infoSet : Encoding =
        let playout =
            match infoSet.Deal.PlayoutOpt with
                | Some playout -> playout
                | None -> failwith "No playout"
        let encoded =
            BitArray [|
                yield! encodeCards infoSet.Hand   // current player's hand
                yield! encodePlayout playout      // playout
            |]
        assert(encoded.Length = encodedLength)
        encoded
