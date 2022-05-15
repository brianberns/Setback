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
            let! handView = DealView.create surface deal

            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
            let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s

                // enable card play
            let animateCardPlay =
                handView |> HandView.playS surface
            for cardView in handView do
                let card = cardView |> CardView.card
                cardView.click(fun () ->
                    deal
                        |> AbstractOpenDeal.addPlay card
                        |> ignore
                    animateCardPlay cardView
                        |> Animation.run
                        |> ignore)
        } |> ignore

    (~~document).ready(run)
