namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

/// Values from -1.0 to 1.0.
type Coord = float

module Coord =

    let toLength (max : Length) (coord : Coord) =
        (0.5 * (float max.NumPixels * (coord + 1.0)))
            |> Pixel

module App =

    let rng = Random()
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    JQuery.init ()

    (~~document).ready (fun () ->

        let surface = JQuery.select "#surface"
        let maxWidth =
            let width =
                surface.css "width"
                    |> Length.parse
            width - CardView.width
        let maxHeight =
            let height =
                surface.css "height"
                    |> Length.parse
            height - CardView.height
        let moveCardView x y cardView =
            let left = Coord.toLength maxWidth x
            let top = Coord.toLength maxHeight y
            cardView |> CardView.moveTo left top

        let init cardView x y =
            surface.append(cardView)
            cardView |> moveCardView x y

        for i = 0 to 51 do
            let cardView =
                Card.allCards.[i]
                    |> CardView.create
            let angle = (float i) * (2.0 * Math.PI / 52.0)
            init cardView (cos angle) (sin angle)
    )
