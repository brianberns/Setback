namespace Setback.Cfrm

open System
open Setback

/// Abstract view of an auction used when bidding.
type AbstractAuction =
    {
        /// Number of bids so far.
        NumBids : int

        /// High bid so far.
        HighBid : AbstractHighBid
    }

module AbstractAuction =

    /// Initial abstract auction state.
    let initial =
        {
            NumBids = 0
            HighBid = AbstractHighBid.none
        }

    /// Indicates whether the given abstract auction is complete.
    let isComplete auction =
        assert(
            auction.NumBids >= 0
                && auction.NumBids <= Seat.numSeats)
        auction.NumBids = Seat.numSeats

    /// Index of the current bidder, relative to the dealer.
    let currentBidderIndex auction =
        assert(auction |> isComplete |> not)
        (auction.NumBids + 1) % Seat.numSeats   // player to dealer's left bids first

    /// What are the allowed bids in the given auction?
    let legalBids auction =
        assert(auction |> isComplete |> not)
        match auction.HighBid.Bid with
            | Bid.Pass -> [| Bid.Pass; Bid.Two; Bid.Three; Bid.Four |]
            | Bid.Two -> [| Bid.Pass; Bid.Three; Bid.Four |]
            | Bid.Three -> [| Bid.Pass; Bid.Four |]
            | Bid.Four ->
                if currentBidderIndex auction = 0 then   // dealer can take four-bid
                    assert(auction.NumBids = Seat.numSeats - 1)
                    [| Bid.Pass; Bid.Four |]
                else
                    [| Bid.Pass |]
            | _ -> failwith "Unexpected"

    /// Adds the given bid to the given abstract auction.
    let addBid bid auction =
        assert(auction |> isComplete |> not)
        let highBid =
            if bid = Bid.Pass then
                auction.HighBid
            else
                assert(bid >= auction.HighBid.Bid)
                let iBidder = currentBidderIndex auction
                assert(   // dealer can steal 4-bid
                    bid > auction.HighBid.Bid
                        || (iBidder = 0 && bid = Bid.Four))
                AbstractHighBid.create iBidder bid
        {
            auction with
                NumBids = auction.NumBids + 1
                HighBid = highBid
        }

    /// String representation of an abstract auction.
    let layout =
        [|
            SpanLayout.ofLength 1   // high bid so far
        |] |> SpanLayout.combine

    /// String representation of an abstract auction.
    let copyTo (span : Span<_>) auction =
        assert(auction |> isComplete |> not)
        assert(span.Length = layout.Length)

            // high bid so far
        let cBid =
            let highBid = auction.HighBid.Bid
            if highBid = Bid.Four
                && currentBidderIndex auction = 0 then 'D'   // dealer can override?
            else highBid |> int |> Char.fromDigit
        layout.Slice(0, span).Fill(cBid)

    type Record =
        {
            HighBid : Bid
            DealerOverride : bool
        }

    let parse (span : Span<_>) =
        assert(span.Length = layout.Length)
        let bidStr = layout.Slice(0, span).ToString()
        {
            HighBid =
                if bidStr = "D" then Bid.Four
                else bidStr |> int |> enum<Bid>
            DealerOverride = bidStr = "D"
        }
