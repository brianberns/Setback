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

    /// Encodes the given (card, value) pairs as a
    /// vector in the deck size.
    let inline encodeCardValues pairs =
        let valueMap =
            pairs
                |> Seq.map (fun (card, value) ->
                    Card.toIndex card, value)
                |> Map
        [|
            for index = 0 to Card.numCards - 1 do
                valueMap
                    |> Map.tryFind index
                    |> Option.defaultValue
                        LanguagePrimitives.GenericZero   // encode to input type
        |]

    /// Encodes the given cards as a multi-hot vector
    /// in the deck size.
    let encodeCards cards =
        let cardIndexes =
            cards
                |> Seq.map Card.toIndex
                |> set
        [|
            for index = 0 to Card.numCards - 1 do
                cardIndexes.Contains(index)
        |]

    /// Encodes each card in the given trick as a one-
    /// hot vector in the deck size and concatenates those
    /// vectors. Since the cards are in reverse order (e.g.
    /// ENW for South about to play), this gets encoded like:
    ///    After 3 plays: ENW  -> WNE
    ///    After 2 plays: EN   -> -NE
    ///    After 1 plays: E    -> --E
    ///    After 0 plays:      -> ---
    let encodeTrick isCurrent trickOpt =
        let cards =
            trickOpt
                |> Option.map (fun trick ->
                    List.toArray trick.Cards)
                |> Option.defaultValue Array.empty
        assert(
            match isCurrent, trickOpt.IsSome with
                | true, true -> cards.Length < Seat.numSeats
                | true, false -> false
                | false, true -> cards.Length = Seat.numSeats
                | false, false -> cards.Length = 0)
        let decr = if isCurrent then 2 else 1
        [|
            for iCard = Seat.numSeats - decr downto 0 do
                yield!
                    if iCard < cards.Length then
                        Some cards[iCard]
                    else None
                    |> Option.toArray
                    |> encodeCards
        |]

    let encodePlayout playout =
        let pairs =
            [|
                match playout.CurrentTrickOpt with
                    | Some trick -> trick, true
                    | None -> failwith "No current trick"
                for trick in playout.CompletedTricks do
                    trick, false
            |]
        [|
            for iTrick = Setback.numCardsPerHand - 1 downto 0 do
                yield!
                    if iTrick < pairs.Length then
                        let trick, isCurrent = pairs[iTrick]
                        encodeTrick isCurrent (Some trick)
                    else
                        encodeTrick false None
        |]

    let encodedCardLength = Card.numCards

    /// Total encoded length of an info set.
    let encodedLength =
        Card.numCards                          // current player's hand
            + ((Setback.numCardsPerDeal - 1)   // tricks so far
                * encodedCardLength)

    /// Encodes the given info set as a vector.
    let encode infoSet : Encoding =
        let playout =
            match infoSet.Deal.PlayoutOpt with
                | Some playout -> playout
                | None -> failwith "No playout"
        let encoded =
            BitArray [|
                yield! encodeCards infoSet.Hand   // current player's hand
                yield! encodePlayout playout      // tricks so far
            |]
        assert(encoded.Length = encodedLength)
        encoded
