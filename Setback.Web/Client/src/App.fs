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

    let addCard card (div : HTMLDivElement) =
        card
            |> Img.ofCard
            |> div.appendChild
            |> ignore

    let surface =
        document.getElementById("surface")
            :?> HTMLDivElement
    for card in Card.allCards do
        addCard card surface
