namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

open Setback
open Setback.Cfrm

module AbstractOpenDeal =

    /// Answers the current player's seat.
    let getCurrentSeat dealer deal =
        Seat.incr
            (AbstractOpenDeal.currentPlayerIndex deal)
            dealer

module Auction =

    /// Auction context.
    type private Context<'t> =
        {
            /// Current dealer's seat.
            Dealer : Seat

            /// Current score, relative to the dealer's team.
            Score : AbstractScore

            /// Current deal.
            Deal : AbstractOpenDeal

            /// Animation of making a bid.
            AnimBid : Bid -> Animation
        }

    /// Makes the given bid in the given deal and then continues
    /// the rest of the deal.
    let private makeBid context bid =
        promise {

                // write to log
            do
                let seat =
                    AbstractOpenDeal.getCurrentSeat
                        context.Dealer
                        context.Deal
                console.log($"{Seat.toString seat} bids {Bid.toString bid}")

                // animate the bid
            do! context.AnimBid bid
                |> Animation.run

                // add bid to deal
            return context.Deal
                |> AbstractOpenDeal.addBid bid
        }

    /// Allows user to make a bid.
    let private bidUser chooser context =

            // determine all legal bids
        let legalBids =
            context.Deal.ClosedDeal.Auction
                |> AbstractAuction.legalBids
                |> set
        assert(legalBids |> Set.isEmpty |> not)

            // enable user to select one of the corresponding bid views
        Promise.create (fun resolve _reject ->
            chooser |> BidChooser.display
            for bidView in chooser.BidViews do
                let bid = bidView |> BidView.bid
                if legalBids.Contains(bid) then
                    bidView.addClass("active")
                    bidView.click(fun () ->

                            // prevent further clicks
                        chooser.Element.remove()

                            // make the selected bid
                        promise {
                            let! value = makeBid context bid
                            resolve value
                        } |> ignore)
                else
                    bidView.prop("disabled", true))

    /// Automatically makes a bid.
    let private bidAuto context =
        async {
                // determine bid to make
            let! bid =
                WebPlayer.makeBid context.Score context.Deal

                // move to next player
            return! makeBid context bid
                |> Async.AwaitPromise
        }

    /// Runs the given deal's auction.
    let run persState score chooser (auctionMap : Map<_, _>) =

        /// Makes a single bid and then loops recursively.
        let rec loop (persState : PersistentState) =
            async {
                let deal = persState.Deal
                let isComplete =
                    deal.ClosedDeal.Auction
                        |> AbstractAuction.isComplete
                if isComplete then
                    return persState
                else
                        // prepare current player
                    let dealer = persState.Dealer
                    let seat =
                        AbstractOpenDeal.getCurrentSeat dealer deal
                    let animBid = auctionMap[seat]
                    let bidder =
                        if seat.IsUser then
                            bidUser chooser >> Async.AwaitPromise
                        else bidAuto

                        // invoke bidder
                    let! deal' =
                        bidder {
                            Dealer = dealer
                            Score = score
                            Deal = deal
                            AnimBid = animBid
                        }

                        // recurse until auction is complete
                    let persState' =
                        { persState with DealOpt = Some deal' }.Save()
                    return! loop persState'
            }

        loop persState
