namespace Hearts

open System
open PlayingCards
open Setback

module Tournament =

    /// Runs a 2v2 tournament between two players.
    let run rngSeed inParallel numGames champion challenger =

        let runWith numGames (challengerSeats : Set<_>) =
            let playerMap =
                Enum.getValues<Seat>
                    |> Seq.map (fun seat ->
                        let player =
                            if challengerSeats.Contains(seat) then
                                challenger
                            else champion
                        seat, player)
                    |> Map
            let rng = Random(rngSeed)
            Game.playGames rng inParallel numGames (   // to-do: avoid creating deals in two different places
                Game.playGame rng playerMap)

            // duplicate deals, so each deal runs twice
        assert(numGames % 2 = 0)
        let halfGames = numGames / 2

            // champion and challenger are represented equally
        assert(Seat.numSeats % 2 = 0)
        let nSeats = Seat.numSeats / 2

        let teamMap =
            [|
                yield! runWith halfGames (set [ Seat.East; Seat.West ])
                yield! runWith halfGames (set [ Seat.North; Seat.South ])
            |]
                |> Seq.groupBy id
                |> Seq.map (fun (team, group) ->
                    team, Seq.length group)
                |> Map
        teamMap
