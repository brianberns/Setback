namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let run deal =
        let surface = CardSurface.init "#surface"
        DealView.create surface deal
            |> Animation.run

    (~~document).ready (fun () ->

        let rng = Random()
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        run deal)
