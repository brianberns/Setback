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
    let private encodeCards cards =
        let cardIndexes =
            cards
                |> Seq.map Card.toIndex
                |> set
        [|
            for index = 0 to Card.numCards - 1 do
                cardIndexes.Contains(index)
        |]

    /// Encodes the given bid as a one-hot vector.
    let private encodeBid (bidOpt : Option<Bid>) =
        let bidIndex =
            bidOpt
                |> Option.map int
                |> Option.defaultValue -1
        [|
            for index = 0 to Bid.numBids - 1 do
                index = bidIndex
        |]

    /// Encodes the given bids as concatenated one-hot
    /// vectors. Since the bids are in reverse order (e.g.
    /// ENW for South about to bid), this gets encoded like:
    ///    After 3 bids: ENW  -> WNE
    ///    After 2 bids: EN   -> -NE
    ///    After 1 bids: E    -> --E
    ///    After 0 bids:      -> ---
    let private encodeBids isCurrent bids =
        let bidArray = Seq.toArray bids
        assert(not isCurrent
            = (bidArray.Length = Seat.numSeats))
        let decr = if isCurrent then 2 else 1
        [|
            for iBid = Seat.numSeats - decr downto 0 do
                yield!
                    if iBid < bidArray.Length then
                        Some bidArray[iBid]
                    else None
                    |> encodeBid
        |]        

    /// Encodes each card in the given trick as a one-
    /// hot vector in the deck size and concatenates those
    /// vectors. Since the cards are in reverse order (e.g.
    /// ENW for South about to play), this gets encoded like:
    ///    After 3 plays: ENW  -> WNE
    ///    After 2 plays: EN   -> -NE
    ///    After 1 plays: E    -> --E
    ///    After 0 plays:      -> ---
    let private encodeTrick isCurrent trick =
        let cards = List.toArray trick.Cards
        assert(cards.Length < Seat.numSeats)
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

    /// Encodes the given voids as a multi-hot vector in the
    /// number of suits times the number of other seats.
    let private encodeVoids player voids =
        [|
            for suit in Enum.getValues<Suit> do
                let seats =
                    Seat.cycle player |> Seq.skip 1
                for seat in seats do
                    Set.contains (seat, suit) voids
        |]

    module Auction =

        /// Total encoded length of an info set.
        let encodedLength =
            Card.numCards                               // current player's hand
                + (Bid.numBids * (Seat.numSeats - 1))   // other players' bids

        /// Encodes the given info set as a vector.
        let encode infoSet : Encoding =
            let encoded =
                BitArray [|
                    yield! encodeCards infoSet.Hand
                    yield! encodeBids
                        true infoSet.Deal.Auction.Bids
                |]
            assert(encoded.Length = encodedLength)
            encoded

    module Playout =

        /// Total encoded length of an info set.
        let encodedLength =
            Card.numCards                                           // current player's hand
                + (Bid.numBids * Seat.numSeats)                     // each player's bids
                + ((Setback.numCardsPerDeal - 1) * Card.numCards)   // tricks so far

        let private encodeTricks playout =
            let pairs =
                [|
                    match playout.CurrentTrickOpt with
                        | Some trick -> trick, true
                        | None -> failwith "No current trick"
                    for trick in playout.CompletedTricks do
                        trick, false
                |]
            [|
                for (trick, isCurrent) in pairs do
                    yield! encodeTrick isCurrent trick
            |]

        /// Encodes the given info set as a vector.
        let encode infoSet : Encoding =
            let bids = infoSet.Deal.Auction.Bids
            let playout =
                match infoSet.Deal.PlayoutOpt with
                    | Some playout -> playout
                    | None -> failwith "No playout"
            let encoded =
                BitArray [|
                    yield! encodeCards infoSet.Hand   // current player's hand
                    yield! encodeBids false bids      // each player's bids
                    yield! encodeTricks playout       // tricks so far
                |]
            assert(encoded.Length = encodedLength)
            encoded
