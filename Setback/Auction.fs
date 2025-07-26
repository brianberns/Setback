namespace Setback

type Auction =
    {
        /// Player who dealt this hand.
        Dealer : Seat

        /// Each player's bid, in reverse chronological order.
        /// Dealer bids last.
        Bids : List<Bid>

        /// Auction winner, if any.
        HighBidderOpt : Option<Seat>

        /// Auction-winning bid (or Pass, if none).
        HighBid : Bid
    }

module Auction =

    /// Creates a new deal.
    let create dealer =
        {
            Dealer = dealer
            Bids = List.empty
            HighBidderOpt = None
            HighBid = Bid.Pass
        }

    let isComplete auction =
        assert(auction.Bids.Length <= Seat.numSeats)
        auction.Bids.Length = Seat.numSeats

    /// Current bidder in the given deal.
    let nextBidder auction =
        assert(not (isComplete auction))
        auction.Dealer
            |> Seat.incr (auction.Bids.Length + 1)

    /// What are the allowed bids in the current situation?
    let legalBids auction =
        let bid = auction.HighBid
        seq {
            yield Bid.Pass
            if bid < Bid.Two then yield Bid.Two
            if bid < Bid.Three then yield Bid.Three
            if bid < Bid.Four then yield Bid.Four
            elif auction.Bids.Length = Seat.numSeats - 1 then
                yield Bid.Four   // dealer can steal 4-bid
        }

    /// Answers a new auction with the next player's given bid.
    let addBid bid auction =
        assert(auction |> legalBids |> Seq.contains bid)

            // is this bid currently winning the auction?
        let bidder = nextBidder auction
        let highBidderOpt, highBid =
            if bid = Bid.Pass then
                auction.HighBidderOpt, auction.HighBid
            else
                Some bidder, bid

        {
            auction with
                Bids = bid :: auction.Bids
                HighBidderOpt = highBidderOpt
                HighBid = highBid
        }
