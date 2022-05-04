namespace Setback.Web.Client

open System

open Browser.Dom
open Browser.Types

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let rng = Random()
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    let surface =
        document.getElementById("surface")
            :?> HTMLDivElement
    for i = 0 to 51 do
        let card = Card.allCards.[i]
        let img =
            CardView.create
                (Length.pct (2 * i)) (Length.pct (2 * i)) (Length.px 75)
                card
        surface.appendChild(img) |> ignore
