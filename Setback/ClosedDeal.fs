namespace Setback

/// Possible points available in a Setback deal.
///
/// The terminology here gets quite confusing. We use the term "deal
/// point" to distinguish these points from "game points". Whichever team
/// gets the most game points in a deal wins the "Game" deal point for
/// that deal.
type DealPoint =
    | High = 0   // highest dealt trump
    | Low  = 1   // lowest dealt trump
    | Jack = 2   // jack of trump (if dealt)
    | Game = 3   // AKQJT of all suits

/// A deal is a round of play within a game. A closed deal is the
/// "public" view of a deal, so it contains no information about
/// how unplayed cards are distributed among the players.
type ClosedDeal =
    {
        Auction : Auction
        PlayoutOpt : Option<Playout>
    }

    member this.Dealer = this.Auction.Dealer

module ClosedDeal =

    /// Creates a new deal.
    let create dealer =
        {
            Auction = Auction.create dealer
            PlayoutOpt = None
        }

    let isComplete deal =
        match deal.PlayoutOpt with
            | Some playout -> Playout.isComplete playout
            | None ->
                deal.Auction.Bids.Length = Seat.numSeats   // all players passed?
                    && deal.Auction.HighBid = Bid.Pass

    let currentPlayer deal =
        match deal.PlayoutOpt with
            | Some playout->
                assert(Auction.isComplete deal.Auction)
                Playout.currentPlayer playout
            | None ->
                assert(Auction.isComplete deal.Auction |> not)
                Auction.currentBidder deal.Auction

    /// Adds the given bid to the given deal.
    let addBid bid deal =
        { deal with
            Auction = Auction.addBid bid deal.Auction }

    /// Plays the given card on the given deal.
    let addPlay card deal =
        assert(Auction.isComplete deal.Auction)
        let playout =
            deal.PlayoutOpt
                |> Option.defaultWith (fun () ->
                    deal.Auction
                        |> Auction.highBidder
                        |> Playout.create)
        { deal with
            PlayoutOpt = Some (Playout.addPlay card playout) }

    let tricks deal =
        deal.PlayoutOpt
            |> Option.map Playout.tricks
            |> Option.defaultValue Seq.empty

    let toString deal =
        let auctionStr = Auction.toString deal.Auction
        deal.PlayoutOpt
            |> Option.map (fun playout ->
                auctionStr + "\r\n" + Playout.toString playout)
            |> Option.defaultValue auctionStr
