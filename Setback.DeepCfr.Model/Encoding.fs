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
    /// vectors. If the current player is South, since the
    /// bids are in reverse order (e.g. ENW), this gets
    /// encoded like:
    ///    4rd bidder: ENW -> WNE
    ///    3nd bidder: EN  -> -NE
    ///    2st bidder: E   -> --E
    ///    1st bidder:     -> ---
    let private encodeBids bids =
        let bidArray = Seq.toArray bids
        [|
            for iBid = Seat.numSeats - 2 downto 0 do
                yield!
                    if iBid < bidArray.Length then
                        Some bidArray[iBid]
                    else None
                    |> encodeBid
        |]        

    /// Encodes each card in the given current trick as
    /// a one-hot vector in the deck size and concatenates
    /// those vectors.
    let private encodeTrick trickOpt =
        let cards =
            trickOpt
                |> Option.map (fun (trick : Trick) ->
                    trick.Cards
                        |> List.toArray)
                |> Option.defaultValue Array.empty
        assert(cards.Length < Seat.numSeats)
        [|
            for iCard = Seat.numSeats - 2 downto 0 do
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
                    yield! encodeBids infoSet.Deal.Auction.Bids
                |]
            assert(encoded.Length = encodedLength)
            encoded

    module Playout =

        /// Encodes the given info set as a vector.
        let encode infoSet : Encoding =
            let unseen =
                infoSet.Deal.UnplayedCards - infoSet.Hand
            let trickOpt = infoSet.Deal.CurrentTrickOpt
            let encoded =
                BitArray [|
                    yield! encodeCards infoSet.Hand             // current player's hand
                    yield! encodeCards unseen                   // unplayed cards not in current player's hand
                    yield! encodeExchangeDirection              // exchange direction
                        infoSet.Deal.ExchangeDirection
                    yield! encodePass infoSet.OutgoingPassOpt   // outgoing pass
                    yield! encodePass infoSet.IncomingPassOpt   // incoming pass
                    yield! encodeTrick trickOpt                 // current trick
                    yield! encodeVoids                          // voids
                        infoSet.Player infoSet.Deal.Voids
                    yield! encodeScore                          // score
                        infoSet.Player infoSet.Deal.Score
                |]
            assert(encoded.Length = encodedLength)
            encoded
