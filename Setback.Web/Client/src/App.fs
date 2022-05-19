namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let run () =

        let surface = CardSurface.init "#surface"

        let rng = Random()
        let dealer = Seat.West
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        promise {

            let! handViews = DealView.start surface dealer deal

            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
            let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
                    
            let playoutMap =
                handViews
                    |> Seq.map (fun (seat, handView) ->

                        let animCardPlay =
                            let play =
                                if seat.IsUser then OpenHandView.play
                                else ClosedHandView.play
                            play surface seat handView

                        let animTrickFinish =
                            TrickView.finish surface

                        seat, (handView, animCardPlay, animTrickFinish))
                    |> Map

            Playout.play dealer playoutMap deal

        } |> ignore

        // start the game when the browser is ready
    (~~document).ready(run)
