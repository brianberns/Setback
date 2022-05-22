namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module Playout =

    /// Answers legal plays in the given hand and deal.
    let private getLegalPlays hand closedDeal =
        match closedDeal.PlayoutOpt with
            | Some playout ->
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Set.ofSeq
            | _ -> failwith "Unexpected"

    /// Playout context.
    type private Context =
        {
            /// Current dealer's seat.
            Dealer : Seat

            /// Current deal.
            Deal : AbstractOpenDeal

            /// Animation of playing a card.
            AnimCardPlay : CardView -> Animation

            /// Animation of winning a trick.
            AnimTrickFinish : Seat -> Animation

            /// Continues the playout.
            Continuation : AbstractOpenDeal -> unit
        }

    /// Plays the given card on the current trick, and returns the
    /// seat of the resulting trick winner, if any.
    let private getTrickWinnerOpt context card =
        assert(context.Deal.ClosedDeal.PlayoutOpt.IsSome)
        option {
                // get trump suit, if any
            let! playout = context.Deal.ClosedDeal.PlayoutOpt
            let! trump = playout.TrumpOpt

                // play card on current trick
            let trick =
                playout.CurrentTrick
                    |> AbstractTrick.addPlay trump card

                // if this card completes the trick, determine winner
            if trick |> AbstractTrick.isComplete then
                return context.Dealer
                    |> Seat.incr (
                        AbstractTrick.highPlayerIndex trick)
        }

    /// Plays the given card in the given deal and then continues
    /// the rest of the deal.
    let private playCard context card =
        promise {

                // write to log
            do
                let seat =
                    AbstractOpenDeal.getCurrentSeat
                        context.Dealer
                        context.Deal
                console.log($"{seat |> Seat.toString} plays {card}")

                // animate if trick is finished
            match getTrickWinnerOpt context card with
                | Some winner ->
                    let animate () =
                        context.AnimTrickFinish winner
                            |> Animation.run
                    if winner.IsUser then   // don't force user to wait for animation to finish
                        animate () |> ignore
                    else
                        do! animate ()
                | None -> ()

                // add the card to the deal
            let deal =
                context.Deal
                    |> AbstractOpenDeal.addPlay card

                // continue the rest of the deal
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout ->
                    if playout |> AbstractPlayout.isComplete then
                        do! AuctionView.removeAnim ()
                            |> Animation.run
                    else
                        deal |> context.Continuation
                | None -> failwith "Unexpected"
        }

    /// Allows user to play a card.
    let private playUser (handView : HandView) context =

            // determine all legal plays
        let legalPlays =
            let hand =
                AbstractOpenDeal.currentHand context.Deal
            getLegalPlays hand context.Deal.ClosedDeal

            // enable user to select one of the corresponding card views
        for cardView in handView do
            let card = cardView |> CardView.card
            if legalPlays.Contains(card) then
                cardView.addClass("active")
                cardView.click(fun () ->

                        // prevent further clicks at this level
                    for cardView in handView do
                        cardView.removeClass("active")
                        cardView.off("click")

                        // play the selected card
                    promise {
                        do! context.AnimCardPlay cardView |> Animation.run
                        do! playCard context card
                    } |> ignore)

    /// Automatically plays a card.
    let private playAuto context =
        async {
            try
                    // determine card to play
                let! card =
                    WebPlayer.makePlay AbstractScore.zero context.Deal

                    // animate playing the selected card
                let cardView = CardView.ofCard card
                do! context.AnimCardPlay cardView
                    |> Animation.run
                    |> Async.AwaitPromise

                    // move to next player
                do! playCard context card
                    |> Async.AwaitPromise

            with ex -> console.log(ex)
        } |> Async.StartImmediate

    /// Plays the given deal.
    let play dealer deal (playoutMap : Map<_, _>) =
        assert(
            deal.ClosedDeal.Auction
                |> AbstractAuction.isComplete)

        /// Plays a single card and then loops recursively.
        let rec loop deal =

                // prepare current player
            let seat =
                AbstractOpenDeal.getCurrentSeat dealer deal
            let (handView : HandView), animCardPlay, animTrickFinish =
                playoutMap.[seat]
            let player =
                if seat.IsUser then
                    playUser handView
                else
                    playAuto

                // invoke player
            player {
                Dealer = dealer
                Deal = deal
                AnimCardPlay = animCardPlay
                AnimTrickFinish = animTrickFinish
                Continuation = loop
            }

        loop deal
