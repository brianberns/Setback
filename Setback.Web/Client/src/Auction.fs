namespace Setback.Web.Client

open Browser.Dom
open Fable.Core
open Setback

module Auction =

    /// Auction context.
    type private Context =
        {
            /// Current game.
            Game : Game

            /// Animation of making a bid.
            AnimBid : Bid -> Animation
        }

    /// Makes the given bid in the given game and then continues
    /// the rest of the game.
    let private makeBid context bid =
        promise {

                // write to log
            do
                let seat =
                    context.Game.Deal |> OpenDeal.currentPlayer
                console.log($"{Seat.toString seat} bids {Bid.toString bid}")

                // animate the bid
            do! context.AnimBid bid |> Animation.run

                // add bid to deal
            return context.Game |> Game.addAction (Choice1Of2 bid)
        }

    /// Allows user to make a bid.
    let private bidUser chooser context =

            // determine all legal bids
        let legalBids =
            context.Game.Deal.ClosedDeal.Auction
                |> Auction.legalBids
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
            let! action = WebPlayer.takeAction context.Game
            let bid = Action.toBid action

                // move to next player
            return! makeBid context bid
                |> Async.AwaitPromise
        }

    /// Runs the given deal's auction.
    let run persState chooser (auctionMap : Map<_, _>) =

        /// Makes a single bid and then loops recursively.
        let rec loop (persState : PersistentState) =
            async {
                    // is auction complete?
                let deal = persState.Game.Deal
                let isComplete =
                    Auction.isComplete deal.ClosedDeal.Auction
                if isComplete then
                    return persState
                else
                        // prepare current player
                    let seat = OpenDeal.currentPlayer deal
                    let animBid = auctionMap[seat]
                    let bidder =
                        if seat.IsUser then
                            bidUser chooser >> Async.AwaitPromise
                        else bidAuto

                        // invoke bidder
                    let! game =
                        bidder {
                            Game = persState.Game
                            AnimBid = animBid
                        }

                        // recurse until auction is complete
                    let persState' =
                        { persState with Game = game }.Save()
                    return! loop persState'
            }

        loop persState
