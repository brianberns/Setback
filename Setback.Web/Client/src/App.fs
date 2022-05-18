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

    /// Plays the given card on the current trick, and returns the
    /// seat of the resulting trick winner, if any.
    let private getTrickWinnerOpt dealer deal card =
        assert(deal.ClosedDeal.PlayoutOpt.IsSome)
        option {
                // get trump suit, if any
            let! playout = deal.ClosedDeal.PlayoutOpt
            let! trump = playout.TrumpOpt

                // play card on current trick
            let trick =
                playout.CurrentTrick
                    |> AbstractTrick.addPlay trump card

                // if this card completes the trick, determine winner
            if trick |> AbstractTrick.isComplete then
                return dealer
                    |> Seat.incr (
                        AbstractTrick.highPlayerIndex trick)
        }

    /// Plays the given deal.
    let play dealer handViewMap deal =

        /// Plays the given card in the given deal and then recursively
        /// plays the rest of the deal.
        let rec addPlay card deal animTrickFinish =
            promise {
                    // animate if trick is finished
                match getTrickWinnerOpt dealer deal card with
                    | Some winner ->
                        do! animTrickFinish winner
                            |> Animation.run
                    | None -> ()

                    // play the card and continue
                deal
                    |> AbstractOpenDeal.addPlay card
                    |> loop
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
                    cardView.addClass("active")
                    cardView.click(fun () ->

                            // prevent further clicks at this level
                        for cardView in handView do
                            cardView.removeClass("active")
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
                            TrickView.finish surface

                        seat, (handView, animPlay, animTrickFinish))
                    |> Map

            play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
