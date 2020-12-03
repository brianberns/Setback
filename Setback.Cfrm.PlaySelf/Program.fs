open System

open Setback
open Setback.Cfrm

/// Session.
let session =
    let dbPlayerEW = DatabasePlayer.player "Setback50.db"
    let dbPlayerNS = DatabasePlayer.player "Setback55.db"
    let playerMap =
        Map [
            Seat.West, dbPlayerEW
            Seat.North, dbPlayerNS
            Seat.East, dbPlayerEW
            Seat.South, dbPlayerNS
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
    let nGames = gamesWon.[0] + gamesWon.[1]
    if nGames % 1000 = 0 then
        printfn $"{nGames}"

[<EntryPoint>]
let main argv =
    session.GameFinishEvent.Add(onGameFinish)
    session.Run(100000)
    printfn $"E+W: {gamesWon.[0]}"
    printfn $"N+S: {gamesWon.[1]}"
    0
