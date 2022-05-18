namespace Setback.Web.Client

open Setback

module TrickView =

    /// Cards played on the current trick.
    let mutable private cardViewMap =
        System.Collections.Generic.Dictionary<Seat, CardView>()

    /// Center point of each card in a trick.
    let private playPointMap =
        Map [
            Seat.West,  Pt (-0.2,  0.0)
            Seat.North, Pt ( 0.0, -0.3)
            Seat.East,  Pt ( 0.2,  0.0)
            Seat.South, Pt ( 0.0,  0.3)
        ]

    /// Animates a card being played on a trick.
    let play surface seat (cardView : CardView) =

            // add card view
        cardViewMap.Add(seat, cardView)

            // animate playing the card
        let pt = playPointMap.[seat]
        surface
            |> CardSurface.getPosition pt
            |> MoveTo
            |> Animation.create cardView

    /// Center point of cards taken in a trick.
    let private finishPointMap =
        Map [
            Seat.West,  Pt (-0.7,  0.1)
            Seat.North, Pt (-0.1, -0.9)
            Seat.East,  Pt ( 0.7, -0.1)
            Seat.South, Pt ( 0.1,  0.9)
        ]

    /// Offset from center of each card taken in a trick.
    let private finishOffsetMap =
        let offset = 0.05
        Map [
            Seat.West,  Pt (-offset,    0.0)
            Seat.North, Pt (   0.0, -offset)
            Seat.East,  Pt ( offset,    0.0)
            Seat.South, Pt (   0.0,  offset)
        ]

    /// Finishes a trick by sending its card to the winner.
    let finish surface winnerSeat =
        assert(cardViewMap.Count = Seat.numSeats)

            // move cards to trick winner
        let step1 =
            let centerPt = finishPointMap.[winnerSeat]
            cardViewMap
                |> Seq.map (fun (KeyValue(seat, cardView)) ->
                    let pt =
                        centerPt + finishOffsetMap.[seat]
                    surface
                        |> CardSurface.getPosition pt
                        |> MoveTo
                        |> Animation.create cardView)
                |> Seq.toArray
                |> Animation.Parallel

            // remove cards from surface
        let step2 =
            cardViewMap
                |> Seq.map (fun (KeyValue(_, cardView)) ->
                    Animation.create cardView Remove)
                |> Seq.toArray
                |> Animation.Parallel

            // reset to new trick
        cardViewMap.Clear()

        [| step1; step2 |]
            |> Animation.Serial
