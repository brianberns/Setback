namespace Setback.Web.Client

open System

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    /// Answers the current player's seat.
    let getCurrentSeat dealer deal =
        let iPlayer =
            deal |> AbstractOpenDeal.currentPlayerIndex
        dealer |> Seat.incr iPlayer

    /// Answers legal plays in the given hand and deal.
    let getLegalPlays hand closedDeal =
        match closedDeal.PlayoutOpt with
            | Some playout ->
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Set.ofSeq
            | _ -> failwith "Unexpected"

    /// Plays the given deal.
    let play dealer handViewMap deal =

        /// Plays the given card in the given deal and then recursively
        /// plays the rest of the deal.
        let rec addPlay card deal animTrickFinish =
            promise {
                let deal' =
                    deal |> AbstractOpenDeal.addPlay card

                let anim =
                    match deal'.ClosedDeal.PlayoutOpt with
                        | Some playout when
                            playout.CurrentTrick.NumPlays = 0 ->
                                animTrickFinish ()
                        | _ -> Animation.None
                do! Animation.run anim

                loop deal'
            }

        /// Allows user to play a card.
        and playHuman (handView : HandView) deal animPlay animTrickFinish =

                // determine all legal plays
            let legalPlays =
                let hand =
                    AbstractOpenDeal.currentHand deal
                getLegalPlays hand deal.ClosedDeal

                // enable user to select one of the corresponding card views
            for cardView in handView do
                let card = cardView |> CardView.card
                if legalPlays.Contains(card) then
                    cardView.click(fun () ->

                            // prevent further clicks at this level
                        for cardView in handView do
                            cardView.off("click")

                        promise {

                                // animate playing the selected card
                            do! animPlay cardView |> Animation.run

                                // move to next player
                            do! addPlay card deal animTrickFinish
                        } |> ignore)

        /// Automatically plays a card.
        and playAuto deal animPlay animTrickFinish =
            async {
                    // determine card to play
                let! card = WebPlayer.makePlay AbstractScore.zero deal

                    // animate playing the selected card
                let cardView = CardView.ofCard card
                do! animPlay cardView
                    |> Animation.run
                    |> Async.AwaitPromise

                    // move to next player
                do! addPlay card deal animTrickFinish
                    |> Async.AwaitPromise
            } |> Async.StartImmediate

        /// Plays entire deal.
        and loop deal =
            let seat = getCurrentSeat dealer deal
            let (handView : HandView), animPlay, animTrickFinish =
                handViewMap |> Map.find seat
            if seat = Seat.South then
                playHuman handView deal animPlay animTrickFinish
            else
                playAuto deal animPlay animTrickFinish

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
                            fun () -> TrickView.finish surface seat

                        seat, (handView, animPlay, animTrickFinish))
                    |> Map

            play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
