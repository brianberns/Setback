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

    /// Creates a new auction.
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

    let highBidder auction =
        match auction.HighBidderOpt with
            | Some bidder -> bidder
            | None -> failwith "No high bidder"

    /// Current bidder in the given auction.
    let currentBidder auction =
        assert(isComplete auction |> not)
        auction.Dealer
            |> Seat.incr (auction.Bids.Length + 1)

    /// All players' bids in chronological order.
    let playerBids auction =
        Seq.zip
            (Seat.cycle auction.Dealer.Next)
            (List.rev auction.Bids)

    /// What are the allowed bids in the current situation?
    let legalBids auction =
        assert(isComplete auction |> not)
        let bid = auction.HighBid
        seq {
            yield Bid.Pass
            if bid < Bid.Two then yield Bid.Two
            if bid < Bid.Three then yield Bid.Three
            if bid < Bid.Four then yield Bid.Four
            elif auction.Bids.Length = Seat.numSeats - 1 then
                yield Bid.Four   // dealer can steal 4-bid
        }

    /// Adds the given bid to the given auction.
    let addBid bid auction =
        assert(auction |> legalBids |> Seq.contains bid)

            // is this bid currently winning the auction?
        let bidder = currentBidder auction
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

    let toString auction =

        let sb = new System.Text.StringBuilder()
        let writeline (s : string) =
            sb.AppendFormat("{0}\r\n", s) |> ignore

        if not auction.Bids.IsEmpty then
            writeline ""
            for (seat, bid) in playerBids auction do
                writeline (sprintf "%-5s: %A" (seat.ToString()) bid)

        sb.ToString()
