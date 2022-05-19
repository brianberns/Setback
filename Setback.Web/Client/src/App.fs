namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let private auction surface dealer deal handViews =
        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass
        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass
        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass
        let deal = deal |> AbstractOpenDeal.addBid Bid.Two
        deal

    let private playout surface dealer deal handViews =
        let playoutMap =
            handViews
                |> Seq.map (fun (seat : Seat, handView) ->

                    let animCardPlay =
                        let play =
                            if seat.IsUser then OpenHandView.play
                            else ClosedHandView.play
                        play surface seat handView

                    let animTrickFinish =
                        TrickView.finish surface

                    seat, (handView, animCardPlay, animTrickFinish))
                |> Map
        Playout.play dealer deal playoutMap

    let private run () =

        let surface = CardSurface.init "#surface"

        let rng = Random()
        let dealer = Seat.West
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        promise {
            let! handViews = DealView.start surface dealer deal
            let deal' = auction surface dealer deal handViews
            playout surface dealer deal' handViews
        } |> ignore

        // start the game when the browser is ready
    (~~document).ready(run)
