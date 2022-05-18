namespace Setback.Web.Client

open System

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    /// Answers the current player's seat.
    let private getCurrentSeat dealer deal =
        Seat.incr
            (AbstractOpenDeal.currentPlayerIndex deal)
            dealer

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

            /// Contines the playout.
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

                // animate if trick is finished
            match getTrickWinnerOpt context card with
                | Some winner ->
                    do! context.AnimTrickFinish winner
                        |> Animation.run
                | None -> ()

                // play the card and continue
            context.Deal
                |> AbstractOpenDeal.addPlay card
                |> context.Continuation
        }

    /// Allows user to play a card.
    let private playHuman (handView : HandView) context =

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
        } |> Async.StartImmediate

    /// Plays the given deal.
    let private play dealer handViewMap deal =

        /// Plays entire deal.
        let rec loop deal =
            let seat = getCurrentSeat dealer deal
            let (handView : HandView), animCardPlay, animTrickFinish =
                handViewMap |> Map.find seat
            let player =
                if seat = Seat.South then
                    playHuman handView
                else
                    playAuto
            player {
                Dealer = dealer
                Deal = deal
                AnimCardPlay = animCardPlay
                AnimTrickFinish = animTrickFinish
                Continuation = loop
            }

        loop deal

    let run () =

        let surface = CardSurface.init "#surface"

        let rng = Random(0)
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        promise {

            let! handViews = DealView.start surface deal

            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
            let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s
                    
            let handViewMap =
                handViews
                    |> Seq.map (fun (seat, handView) ->

                        let animPlay =
                            let play =
                                if seat = Seat.South then
                                    OpenHandView.play
                                else
                                    ClosedHandView.play
                            play surface seat handView

                        let animTrickFinish =
                            TrickView.finish surface

                        seat, (handView, animPlay, animTrickFinish))
                    |> Map

            play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
