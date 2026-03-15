namespace Setback

open System
open PlayingCards

module Tournament =

    /// Runs a 2v2 tournament between two players.
    let run rngSeed inParallel numGames champion challenger =

        let runWith numGames (challengerTeam : Team) =
            let playerMap =
                Enum.getValues<Seat>
                    |> Seq.map (fun seat ->
                        let player =
                            let isChallenger =
                                Team.seats challengerTeam
                                    |> Set.contains seat
                            if isChallenger then challenger
                            else champion
                        seat, player)
                    |> Map
            let rng = Random(rngSeed)
            Game.playGames rng inParallel numGames (   // to-do: avoid creating deals in two different places
                Game.playGame rng playerMap)
                |> Seq.where (fun team -> team = challengerTeam)
                |> Seq.length

            // duplicate deals, so each deal runs twice
        assert(numGames % 2 = 0)
        let halfGames = numGames / 2

            // champion and challenger are represented equally
        assert(Seat.numSeats % 2 = 0)
        let nSeats = Seat.numSeats / 2

        Enum.getValues<Team>
            |> Array.sumBy (runWith halfGames)
