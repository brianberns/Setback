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

    let getLegalPlays hand closedDeal =
        match closedDeal.PlayoutOpt with
            | Some playout ->
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Set.ofSeq
            | _ -> failwith "Unexpected"

    let play dealer handViewMap deal =
        let rec loop deal =
            let seat = getCurrentSeat dealer deal
            let (handView : HandView), anim =
                handViewMap |> Map.find seat
            if seat = Seat.South then
                let hand =
                    AbstractOpenDeal.currentHand deal
                let legalPlays =
                    getLegalPlays hand deal.ClosedDeal
                for cardView in handView do
                    let card = cardView |> CardView.card
                    if legalPlays.Contains(card) then
                        cardView.click(fun () ->
                            promise {
                                do! anim cardView
                                    |> Animation.run
                                deal
                                    |> AbstractOpenDeal.addPlay card
                                    |> loop
                            } |> ignore)
            else
                async {
                    let! card = WebPlayer.makePlay AbstractScore.zero deal
                    let cardView = CardView.ofCard card
                    do! anim cardView
                        |> Animation.run
                        |> Async.AwaitPromise
                    deal
                        |> AbstractOpenDeal.addPlay card
                        |> loop
                } |> Async.StartImmediate
        loop deal

    let run () =

        let surface = CardSurface.init "#surface"

        let rng = Random()
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        promise {
            let! closedW, closedN, closedE, openS = DealView.create surface deal

            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
            let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s

            let handViewMap =
                Map [
                    Seat.West,  (closedW, closedW |> ClosedHandView.playW surface)
                    Seat.North, (closedN, closedN |> ClosedHandView.playN surface)
                    Seat.East,  (closedW, closedE |> ClosedHandView.playE surface)
                    Seat.South, (openS,   openS |> OpenHandView.playS surface)
                ]

            play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
