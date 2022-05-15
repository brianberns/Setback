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
            let! closedW, closedN, closedE, openS = DealView.create surface deal

            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
            let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
            let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s

                // enable card play
            let animateCardPlay =
                openS |> OpenHandView.playS surface
            for cardView in openS do
                let card = cardView |> CardView.card
                cardView.click(fun () ->
                    deal
                        |> AbstractOpenDeal.addPlay card
                        |> ignore
                    animateCardPlay cardView
                        |> Animation.run
                        |> ignore)

            let card = deal.UnplayedCards.[1] |> Seq.head
            let cardView = CardView.ofCard card
            let anim =
                ClosedHandView.playW surface closedW cardView
            do! Animation.run anim

        } |> ignore

    (~~document).ready(run)
