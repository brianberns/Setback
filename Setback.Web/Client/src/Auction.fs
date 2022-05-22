namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module AbstractOpenDeal =

    /// Answers the current player's seat.
    let getCurrentSeat dealer deal =
        Seat.incr
            (AbstractOpenDeal.currentPlayerIndex deal)
            dealer

module AuctionView =

    let private displayMap =
        Map [
            Seat.West,  Pt (-0.9,  0.0)
            Seat.North, Pt (-0.3, -0.9)
            Seat.East,  Pt ( 0.9,  0.0)
            Seat.South, Pt (-0.3,  0.9)
        ]

    let bidAnim surface seat bid =

        let bidView = BidView.ofBid bid
        let origin =
            surface |> CardSurface.getPosition Point.origin
        JQueryElement.setPosition origin bidView
        surface.Element.append(bidView)

        let dest =
            surface |> CardSurface.getPosition displayMap.[seat]
        AnimationAction.moveTo dest
            |> ElementAction.create bidView
            |> Animation.Unit

module Auction =

    /// Auction context.
    type private Context =
        {
            /// Current dealer's seat.
            Dealer : Seat

            /// Current deal.
            Deal : AbstractOpenDeal

            /// Function that allows the user to choose a bid. First
            /// argument is a handler that's invoked when a user chooses
            /// a bid. Second argument is the (non-empty) set of valid
            /// bids from which the user is to choose one.
            ChooseBid : (Bid -> unit) -> Set<Bid> -> unit

            AnimBid : Bid -> Animation

            /// Continues the auction.
            Continuation : AbstractOpenDeal -> unit

            /// Completes the auction (by playing the cards).
            Complete : AbstractOpenDeal -> unit
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
            let deal =
                context.Deal
                    |> AbstractOpenDeal.addBid bid

                // continue the rest of the deal
            let cont =
                if deal.ClosedDeal.Auction |> AbstractAuction.isComplete then
                    context.Complete
                else
                    context.Continuation
            cont deal
        }

    /// Allows user to make a bid.
    let private bidUser context =
        context.Deal.ClosedDeal.Auction
            |> AbstractAuction.legalBids
            |> set
            |> context.ChooseBid (fun bid ->
                promise {
                    do! makeBid context bid
                } |> ignore)

    /// Automatically makes a bid.
    let private bidAuto context =
        async {
            try
                    // determine bid to make
                let! bid =
                    WebPlayer.makeBid AbstractScore.zero context.Deal

                    // move to next player
                do! makeBid context bid
                    |> Async.AwaitPromise

            with ex -> console.log(ex)
        } |> Async.StartImmediate

    /// Runs the given deal's auction.
    let run dealer deal chooseBid (auctionMap : Map<_, _>) cont =

        /// Makes a single bid and then loops recursively.
        let rec loop deal =

                // prepare current player
            let seat =
                AbstractOpenDeal.getCurrentSeat dealer deal
            let animBid =
                auctionMap.[seat]
            let bidder =
                if seat.IsUser then bidUser
                else bidAuto

                // invoke bidder
            bidder {
                Dealer = dealer
                Deal = deal
                ChooseBid = chooseBid
                AnimBid = animBid
                Continuation = loop
                Complete = cont
            }

        loop deal
