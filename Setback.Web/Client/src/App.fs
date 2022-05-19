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
                                if seat.IsUser then
                                    OpenHandView.play
                                else
                                    ClosedHandView.play
                            play surface seat handView

                        let animTrickFinish =
                            TrickView.finish surface

                        seat, (handView, animPlay, animTrickFinish))
                    |> Map

            Playout.play dealer handViewMap deal

        } |> ignore

    (~~document).ready(run)
