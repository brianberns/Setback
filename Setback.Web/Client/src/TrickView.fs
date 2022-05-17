namespace Setback.Web.Client

open Setback

type TrickView = CardView[]

module TrickView =

    /// Coordinates of each card in a trick.
    let private coordsMap =
        Map [
            Seat.West,  (-0.2,  0.0)
            Seat.North, ( 0.0, -0.3)
            Seat.East,  ( 0.2,  0.0)
            Seat.South, ( 0.0,  0.3)
        ]

    /// Animates a card being played on a trick.
    let play surface seat (cardView : CardView) =
        let coords = coordsMap.[seat]
        surface
            |> CardSurface.getPosition coords
            |> MoveTo
            |> Animation.create cardView
