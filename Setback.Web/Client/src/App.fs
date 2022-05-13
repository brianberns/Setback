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

        DealView.create surface deal
            |> Animation.run
            |> ignore

        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // w
        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // n
        let deal = deal |> AbstractOpenDeal.addBid Bid.Pass   // e
        let deal = deal |> AbstractOpenDeal.addBid Bid.Two    // s

        ()

    (~~document).ready(run)
