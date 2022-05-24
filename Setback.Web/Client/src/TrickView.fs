namespace Setback.Web.Client

open Setback

module TrickView =

    /// Cards played on the current trick.
    /// ASSUMPTION: Only one trick view per app.
    let mutable private cardViewMap =
        System.Collections.Generic.Dictionary<Seat, CardView>()

    /// Center point of each card played in a trick.
    let private playPointMap =
        Map [
            Seat.West,  Pt (-0.2,  0.0)
            Seat.North, Pt ( 0.0, -0.3)
            Seat.East,  Pt ( 0.2,  0.0)
            Seat.South, Pt ( 0.0,  0.3)
        ]

    /// Animates a card being played on a trick.
    let playAnim surface seat (cardView : CardView) =

            // add card view
        cardViewMap.Add(seat, cardView)

            // animate playing the card
        let pt = playPointMap.[seat]
        surface
            |> CardSurface.getPosition pt
            |> AnimationAction.moveTo
            |> Animation.create cardView

    /// Center point of cards taken in a trick.
    let private finishPointMap =
        let offsetX = 0.4
        let offsetY = 0.7
        Map [
            Seat.West,  Pt (    -0.7,  offsetY)
            Seat.North, Pt (-offsetX,     -0.9)
            Seat.East,  Pt (     0.7, -offsetY)
            Seat.South, Pt ( offsetX,      0.9)
        ]

    /// Offset from center of each card taken in a trick.
    let private finishOffsetMap =
        let offsetX = 0.05
        let offsetY = 0.1
        Map [
            Seat.West,  Pt (-offsetX,      0.0)
            Seat.North, Pt (     0.0, -offsetY)
            Seat.East,  Pt ( offsetX,      0.0)
            Seat.South, Pt (     0.0,  offsetY)
        ]

    /// Animates the end of a trick by sending its cards to the
    /// trick winner.
    let finishAnim surface winnerSeat =
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
                        |> AnimationAction.moveTo
                        |> Animation.create cardView)
                |> Seq.toArray
                |> Animation.Parallel

            // wait
        let step2 = Animation.Sleep 1500

            // remove cards from surface
        let step3 =
            cardViewMap
                |> Seq.map (fun (KeyValue(_, cardView)) ->
                    Animation.create cardView Remove)
                |> Seq.toArray
                |> Animation.Parallel

            // reset to new trick
        cardViewMap.Clear()

        [| step1; step2; step3 |]
            |> Animation.Serial
