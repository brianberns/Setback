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

    /// Encodes the given card as a single-hot vector in
    /// the deck size.
    let encodeCard cardOpt =
        cardOpt
            |> Option.toArray
            |> encodeCards

    let private encodedSuitLength = Suit.numSuits

    /// Encodes the given suit as a one-hot vector in the
    /// number of seats.
    let private encodeSuit (suitOpt : Option<Suit>) =
        [|
            for suit in Suit.allSuits do
                Some suit = suitOpt
        |]

    let private encodedSeatLength = Seat.numSeats

    /// Encodes the given seat as a one-hot vector in the
    /// number of seats.
    let private encodeSeat (seatOpt : Option<Seat>) =
        [|
            for seat in Seat.allSeats do
                Some seat = seatOpt
        |]

    let private encodedCurrentTrickLength =
        (Seat.numSeats - 1) * (encodedCardLength + 1)

    /// Encodes each card in the given trick as a one-hot
    /// vector and concatenates those vectors.
    let private encodeTrick trick =
        assert(Trick.isComplete trick |> not)

        let cards = Seq.toArray trick.Cards
        let highCardOpt = Option.map snd trick.HighPlayOpt
        let encoded =
            [|
                for iCard = 0 to Seat.numSeats - 2 do
                    let cardOpt =
                        if iCard < cards.Length then
                            Some cards[iCard]
                        else None
                    yield! encodeCard cardOpt
                    yield
                        match cardOpt, highCardOpt with
                            | Some card, Some highCard ->
                                card = highCard
                            | _ -> false
            |]

        assert(encoded.Length = encodedCurrentTrickLength)
        encoded

    let private encodedVoidLength =
        (Seat.numSeats - 1) * Suit.numSuits

    /// Encodes the given voids as a multi-hot vector in the
    /// number of suits times the number of other seats.
    let private encodeVoids player voids =
        let encoded =
            [|
                for suit in Suit.allSuits do
                    let seats =
                        Seat.cycle player |> Seq.skip 1
                    for seat in seats do
                        Set.contains (seat, suit) voids
            |]
        assert(encoded.Length = encodedVoidLength)
        encoded

    let private encodedPlayoutLength =
        encodedSuitLength
            + encodedCurrentTrickLength
            // + encodedVoidLength

    let private encodePlayout playout =
        let player = Playout.currentPlayer playout
        let trick = Playout.currentTrick playout
        let encoded =
            [|
                yield! encodeSuit playout.TrumpOpt
                yield! encodeTrick trick
                // yield! encodeVoids player playout.Voids
            |]
        assert(encoded.Length = encodedPlayoutLength)
        encoded

    /// Total encoded length of an info set.
    let encodedLength =
        encodedCardLength
            + encodedPlayoutLength

    /// Encodes the given info set as a vector.
    let encode infoSet : Encoding =
        let playout =
            match infoSet.Deal.PlayoutOpt with
                | Some playout -> playout
                | None -> failwith "No playout"
        let encoded =
            BitArray [|
                yield! encodeCards infoSet.Hand
                yield! encodePlayout playout
            |]
        assert(encoded.Length = encodedLength)
        encoded
