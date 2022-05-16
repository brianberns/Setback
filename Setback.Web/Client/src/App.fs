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
                        let play =
                            if seat = Seat.South then
                                OpenHandView.play
                            else
                                ClosedHandView.play
                        seat, (handView, play surface seat handView))
                    |> Map

            play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
