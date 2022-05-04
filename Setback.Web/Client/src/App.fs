namespace Setback.Web.Client

open System

open Browser.Dom
open Browser.Types

open Feliz

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

    let div =
        Html.div [
            for i = 0 to 51 do
                let card = Card.allCards.[i]
                yield
                    CardView.create
                        (length.perc (2*i))
                        (length.perc (2*i))
                        (length.px 75)
                        card
        ]

    ReactDOM.render(div, surface)
