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

    /// Initial auction state.
    let initial =
        {
            NumBids = 0
            HighBid = AbstractHighBid.none
        }

    /// Indicates whether the given auction is complete.
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
                if auction.NumBids = Seat.numSeats - 1 then   // dealer can take four-bid
                    assert(currentBidderIndex auction = 0)
                    [| Bid.Pass; Bid.Four |]
                else
                    [| Bid.Pass |]
            | _ -> failwith "Unexpected"

    /// Adds the given bid to the given auction.
    let addBid bid auction =
        assert(auction |> legalBids |> Seq.contains bid)
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

    /// String representation of an auction.
    let layout =
        [|
            SpanLayout.ofLength 1   // high bid so far
        |] |> SpanLayout.combine

    /// String representation of an auction.
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
