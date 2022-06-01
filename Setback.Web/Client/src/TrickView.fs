namespace Setback.Web.Client

open Setback

module TrickView =

    /// Cards played on the current trick.
    /// ASSUMPTION: Only one trick view per app.
    let mutable private cardViewMap =
        System.Collections.Generic.Dictionary<Seat, CardView>()

    /// Center location of each card played in a trick.
    let private playLocMap =
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
        playLocMap.[seat]
            |> AnimationAction.moveTo
            |> Animation.create cardView

    /// Center point of cards taken in a trick.
    let private finishLocMap =
        Position.seatMap [
            Seat.West,  (20, 50)
            Seat.North, (50, 20)
            Seat.East,  (80, 50)
            Seat.South, (50, 80)
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
            let centerLoc = finishLocMap.[winnerSeat]
            cardViewMap
                |> Seq.map (fun (KeyValue(seat, cardView)) ->
                    centerLoc + finishOffsetMap.[seat]
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
