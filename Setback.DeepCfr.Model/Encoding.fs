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

    let encodedSuitLength = Suit.numSuits

    /// Encodes the given suit as a one-hot vector in the
    /// number of seats.
    let encodeSuit (suitOpt : Option<Suit>) =
        [|
            for suit in Suit.allSuits do
                Some suit = suitOpt
        |]

    let encodedSeatLength = Seat.numSeats

    /// Encodes the given seat as a one-hot vector in the
    /// number of seats.
    let encodeSeat (seatOpt : Option<Seat>) =
        [|
            for seat in Seat.allSeats do
                Some seat = seatOpt
        |]

    let encodedCurrentTrickLength =
        (Seat.numSeats - 1) * encodedCardLength

    /// Encodes each card in the given trick as a one-hot
    /// vector and concatenates those vectors.
    let encodeTrick trick =
        assert(Trick.isComplete trick |> not)
        let cards = Seq.toArray trick.Cards
        let encoded =
            [|
                for iCard = 0 to Seat.numSeats - 2 do
                    let cards =
                        if iCard < cards.Length then
                            Some cards[iCard]
                        else None
                        |> Option.toArray
                    yield! encodeCards cards
            |]
        assert(encoded.Length = encodedCurrentTrickLength)
        encoded

    let encodedPlayoutLength =
        encodedSuitLength + encodedCurrentTrickLength

    let encodePlayout playout =
        let trick = Playout.currentTrick playout
        let encoded =
            [|
                yield! encodeSuit playout.TrumpOpt
                yield! encodeTrick trick
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
