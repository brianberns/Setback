namespace Setback.Web.Client

open System.Collections.Generic
open Setback

module TrickView =

    let mutable private cardViewMap = Dictionary<Seat, CardView>()

    /// Coordinates of each card in a trick.
    let private playCoordsMap =
        Map [
            Seat.West,  (-0.2,  0.0)
            Seat.North, ( 0.0, -0.3)
            Seat.East,  ( 0.2,  0.0)
            Seat.South, ( 0.0,  0.3)
        ]

    /// Animates a card being played on a trick.
    let play surface seat (cardView : CardView) =

            // add card view
        cardViewMap.Add(seat, cardView)

            // animate playing the card
        let coords = playCoordsMap.[seat]
        surface
            |> CardSurface.getPosition coords
            |> MoveTo
            |> Animation.create cardView

    let private finishCoordsMap =
        Map [
            Seat.West,  (-0.7,  0.1)
            Seat.North, (-0.1, -0.9)
            Seat.East,  ( 0.7, -0.1)
            Seat.South, ( 0.1,  0.9)
        ]

    let private finishDeltaMap =
        let delta = 0.05
        Map [
            Seat.West,  (-delta,    0.0)
            Seat.North, (   0.0, -delta)
            Seat.East,  ( delta,    0.0)
            Seat.South, (   0.0,  delta)
        ]

    let finish surface winnerSeat =
        assert(cardViewMap.Count = Seat.numSeats)

        let step1 =
            let coords = finishCoordsMap.[winnerSeat]
            cardViewMap
                |> Seq.map (fun (KeyValue(seat, cardView)) ->
                    let coords' =
                        let deltaCoords = finishDeltaMap.[seat]
                        fst coords + fst deltaCoords,
                        snd coords + snd deltaCoords
                    surface
                        |> CardSurface.getPosition coords'
                        |> MoveTo
                        |> Animation.create cardView)
                |> Seq.toArray
                |> Animation.Parallel

        let step2 =
            cardViewMap
                |> Seq.map (fun (KeyValue(_, cardView)) ->
                    Animation.create cardView Remove)
                |> Seq.toArray
                |> Animation.Parallel

        cardViewMap.Clear()

        [| step1; step2 |]
            |> Animation.Serial
