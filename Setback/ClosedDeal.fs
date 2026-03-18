namespace Setback

/// A deal is a round of play within a game. A closed deal is the
/// "public" view of a deal, so it contains no information about
/// how unplayed cards are distributed among the players.
type ClosedDeal =
    {
        /// Auction phase.
        Auction : Auction

        /// Playout phase, if it has started.
        PlayoutOpt : Option<Playout>
    }

    /// Player who dealt this hand.
    member this.Dealer = this.Auction.Dealer

    /// Trump suit, if it's been established.
    member deal.TrumpOpt =
        option {
            let! playout = deal.PlayoutOpt
            return! playout.TrumpOpt
        }

module ClosedDeal =

    /// Creates a new deal.
    let create dealer =
        {
            Auction = Auction.create dealer
            PlayoutOpt = None
        }

    /// Indicates whether the given deal has finished.
    let isComplete deal =
        match deal.PlayoutOpt with
            | Some playout -> Playout.isComplete playout
            | None -> Auction.isComplete deal.Auction   // all players passed?

    /// Current player in the given deal.
    let currentPlayer deal =
        match deal.PlayoutOpt with
            | Some playout ->
                assert(Auction.isComplete deal.Auction)
                Playout.currentPlayer playout
            | None ->
                assert(Auction.isComplete deal.Auction |> not)
                Auction.currentBidder deal.Auction

    /// Adds the given bid to the given deal.
    let addBid bid deal =
        let auction = Auction.addBid bid deal.Auction
        let playoutOpt =
            if Auction.isComplete auction then
                auction.HighBidderOpt
                    |> Option.map Playout.create
            else None
        { deal with
            Auction = auction
            PlayoutOpt = playoutOpt }

    /// Plays the given card on the given deal.
    let addPlay card deal =
        assert(Auction.isComplete deal.Auction)
        let playout =
            match deal.PlayoutOpt with
                | Some playout -> Playout.addPlay card playout
                | None -> failwith "No playout"
        { deal with PlayoutOpt = Some playout }
