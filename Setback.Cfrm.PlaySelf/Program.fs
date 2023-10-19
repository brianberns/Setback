open System

open Setback
open Setback.Cfrm

/// Session.
let session =
    let dbPlayerChampion = DatabasePlayer.player "Champion.db"
    let dbPlayerChallenger = DatabasePlayer.player "Challenger.db"
    let playerMap =
        Map [
            Seat.West, dbPlayerChampion
            Seat.North, dbPlayerChallenger
            Seat.East, dbPlayerChampion
            Seat.South, dbPlayerChallenger
        ]
    let rng = Random(0)
    Session(playerMap, rng)

/// Tracks games won.
let mutable gamesWon = AbstractScore.zero

/// A game has finished.
let onGameFinish (dealer, score) =

        // increment winning team's score
    Game.winningTeamIdxOpt dealer score
        |> Option.iter (fun iTeam ->
            let incr = AbstractScore.forTeam iTeam 1
            gamesWon <- gamesWon + incr)

        // report progress
    let nGames = gamesWon[0] + gamesWon[1]
    if nGames % 1000 = 0 then
        printfn $"{nGames}"

[<EntryPoint>]
let main argv =
    session.GameFinishEvent.Add(onGameFinish)
    let nGamePairs = 100000
    session.Run(nGamePairs)
    printfn $"Defending champion: {gamesWon[0]}, {float gamesWon[0] / (2.0 * float nGamePairs)}"
    printfn $"Challenger: {gamesWon[1]}, {float gamesWon[1] / (2.0 * float nGamePairs)}"
    0
