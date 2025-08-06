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

    let private encodedSuitLength = Suit.numSuits

    /// Encodes the given suit as a one-hot vector in the
    /// number of seats.
    let private encodeSuit suitOpt =
        let encoded =
            [|
                for suit in Suit.allSuits do
                    Some suit = suitOpt
            |]
        assert(encoded.Length = encodedSuitLength)
        encoded

    let encodedCardsLength = Card.numCards

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
        assert(encoded.Length = encodedCardsLength)
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
        assert(encoded.Length = encodedCardsLength)
        encoded

    /// Encodes the given card as a single-hot vector in
    /// the deck size.
    let encodeCard cardOpt =
        cardOpt
            |> Option.toArray
            |> encodeCards

    let private encodedCurrentTrickLength =
        (Seat.numSeats - 1) * encodedCardsLength

    /// Encodes each card in the given trick as one-hot
    /// vectors, and concatenates those vectors.
    let private encodeCurrentTrick trick =
        assert(Trick.isComplete trick |> not)
        let cards = Seq.toArray trick.Cards
        let encoded =
            [|
                for iCard = 0 to Seat.numSeats - 2 do   // exclude the current player
                    yield!
                        if iCard < cards.Length then
                            Some cards[iCard]
                        else None
                        |> encodeCard
            |]
        assert(encoded.Length = encodedCurrentTrickLength)
        encoded

    let private encodedPlayoutLength =
        encodedSuitLength                 // trump
            + encodedCurrentTrickLength   // current trick

    let private encodePlayout playout =
        let trick = Playout.currentTrick playout
        let encoded =
            [|
                yield! encodeSuit playout.TrumpOpt
                yield! encodeCurrentTrick trick
            |]
        assert(encoded.Length = encodedPlayoutLength)
        encoded

    /// Total encoded length of an info set.
    let encodedLength =
        encodedCardsLength           // current player's hand
            + encodedPlayoutLength   // playout 

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
