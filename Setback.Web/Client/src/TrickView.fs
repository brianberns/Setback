namespace Setback.Web.Client

open Setback

module TrickView =

    /// Cards played on the current trick.
    /// ASSUMPTION: Only one trick view per app.
    let mutable private cardViewMap =
        System.Collections.Generic.Dictionary<Seat, CardView>()

    /// Center position of each card played in a trick.
    let private playPosMap =
        Position.seatMap [
            Seat.West,  (45, 50)
            Seat.North, (50, 45)
            Seat.East,  (55, 50)
            Seat.South, (50, 55)
        ]

    /// Animates a card being played on a trick.
    let playAnim seat (cardView : CardView) =

            // add card view
        cardViewMap.Add(seat, cardView)

            // animate playing the card
        playPosMap[seat]
            |> AnimationAction.moveTo
            |> Animation.create cardView

    /// Center position of cards taken in a trick.
    let private finishPosMap =
        Position.seatMap [
            Seat.West,  (15, 70)
            Seat.North, (30, 15)
            Seat.East,  (85, 30)
            Seat.South, (70, 85)
        ]

    /// Offset from center of each card taken in a trick.
    let private finishOffsetMap =
        Position.seatMap [
            Seat.West,  (-5,  0)
            Seat.North, ( 0, -5)
            Seat.East,  ( 5,  0)
            Seat.South, ( 0,  5)
        ]

    /// Animates the end of a trick by sending its cards to the
    /// trick winner.
    let finishAnim winnerSeat =
        assert(cardViewMap.Count = Seat.numSeats)

            // move cards to trick winner
        let step1 =
            let centerLoc = finishPosMap[winnerSeat]
            cardViewMap
                |> Seq.map (fun (KeyValue(seat, cardView)) ->
                    centerLoc + finishOffsetMap[seat]
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
