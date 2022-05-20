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

module Auction =

    /// Auction context.
    type private Context =
        {
            /// Current dealer's seat.
            Dealer : Seat

            /// Current deal.
            Deal : AbstractOpenDeal

            /// Continues the auction.
            Continuation : AbstractOpenDeal -> unit

            /// Completes the auction (by playing the cards).
            Complete : AbstractOpenDeal -> unit
        }

    /// Makes the given bid in the given deal and then continues
    /// the rest of the deal.
    let private makeBid context bid =
        promise {

            do
                let seat =
                    AbstractOpenDeal.getCurrentSeat
                        context.Dealer
                        context.Deal
                console.log($"{seat |> Seat.toString} bids {bid |> Bid.toString}")

            let deal' =
                context.Deal
                    |> AbstractOpenDeal.addBid bid
            let cont =
                if deal'.ClosedDeal.Auction |> AbstractAuction.isComplete then
                    context.Complete
                else
                    context.Continuation
            cont deal'
        }

    /// Allows user to make a bid.
    let private bidUser (handView : HandView) context =
        async {
                // determine bid to make
            let! bid =
                WebPlayer.makeBid AbstractScore.zero context.Deal

                // move to next player
            do! makeBid context bid
                |> Async.AwaitPromise
        } |> Async.StartImmediate

    /// Automatically makes a bid.
    let private bidAuto context =
        async {
                // determine bid to make
            let! bid =
                WebPlayer.makeBid AbstractScore.zero context.Deal

                // move to next player
            do! makeBid context bid
                |> Async.AwaitPromise
        } |> Async.StartImmediate

    /// Runs the given deal's auction.
    let run dealer deal (auctionMap : Map<_, _>) cont =

        /// Makes a single bid and then loops recursively.
        let rec loop deal =

                // prepare current player
            let seat =
                AbstractOpenDeal.getCurrentSeat dealer deal
            let (handView : HandView) =
                auctionMap.[seat]
            let bidder =
                if seat.IsUser then
                    bidUser handView
                else
                    bidAuto

                // invoke bidder
            bidder {
                Dealer = dealer
                Deal = deal
                Continuation = loop
                Complete = cont
            }

        loop deal
