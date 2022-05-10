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
            |> int
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

        let init cardStr x y =
            let cardView =
                cardStr
                    |> Card.fromString
                    |> CardView.create
            surface.append(cardView)
            cardView |> moveCardView x y

        init "AC" -1.0 -1.0
        init "AS" -1.0  1.0
        init "2D"  0.0  0.0
        init "AH"  1.0 -1.0
        init "AD"  1.0  1.0
    )
