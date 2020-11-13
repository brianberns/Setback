namespace Setback.Cfrm

open Setback

/// Abstract view of a closed deal (i.e. excluding unplayed cards,
/// which are kept private by each player).
type AbstractClosedDeal =
    {
        /// Auction phase of this deal.
        Auction : AbstractAuction

        /// Playout phase of this deal, if it has started.
        PlayoutOpt : Option<AbstractPlayout>
    }

module AbstractClosedDeal =

    /// Initial closed deal state.
    let initial =
        {
            Auction = AbstractAuction.initial
            PlayoutOpt = None
        }

    /// Indicates whether the given deal is complete.
    let isComplete closedDeal =
        match closedDeal.PlayoutOpt with
            | None ->
                let auction = closedDeal.Auction
                (AbstractAuction.isComplete auction)
                    && auction.HighBid.Bid = Bid.Pass
            | Some playout ->
                playout |> AbstractPlayout.isComplete

    /// Index of the current player, relative to the dealer.
    let currentPlayerIndex closedDeal =
        match closedDeal.PlayoutOpt with
            | None ->
                closedDeal.Auction
                    |> AbstractAuction.currentBidderIndex
            | Some playout ->
                playout
                    |> AbstractPlayout.currentPlayerIndex

    /// Actions available to the current player with the given
    /// hand in the given deal.
    let getActions hand handLowTrumpRankOpt closedDeal =
        match closedDeal.PlayoutOpt with
            | None ->
                closedDeal.Auction
                    |> BidAction.getActions hand
                    |> Array.map DealBidAction
            | Some playout ->
                playout
                    |> PlayAction.getActions
                        hand
                        handLowTrumpRankOpt
                    |> Array.map DealPlayAction

    /// Adds a bid to the auction of the given deal.
    let addBid bid closedDeal =
        assert(closedDeal.PlayoutOpt.IsNone)
        let auction =
            closedDeal.Auction
                |> AbstractAuction.addBid bid
        let playoutOpt =
            let highBid = auction.HighBid
            if auction |> AbstractAuction.isComplete && highBid.Bid > Bid.Pass then
                highBid
                    |> AbstractPlayout.create
                    |> Some
            else None
        {
            closedDeal with
                Auction = auction
                PlayoutOpt = playoutOpt
        }

    /// Plays a card in the given deal.
    let addPlay card closedDeal =
        assert(closedDeal.PlayoutOpt.IsSome)
        {
            closedDeal with
                PlayoutOpt =
                    closedDeal.PlayoutOpt
                        |> Option.map (AbstractPlayout.addPlay card)
        }
