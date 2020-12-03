﻿open System

open Setback
open Setback.Cfrm

let dbPlayerEW = DatabasePlayer.player "Setback50.db"
let dbPlayerNS = DatabasePlayer.player "Setback55.db"

let session =
    let playerMap =
        Map [
            Seat.West, dbPlayerEW
            Seat.North, dbPlayerNS
            Seat.East, dbPlayerEW
            Seat.South, dbPlayerNS
        ]
    let rng = Random(0)
    Session(playerMap, rng)

/// Shifts from dealer-relative to absolute score.
let shiftScore dealer score =
    let iDealerTeam =
        int dealer % Setback.numTeams
    let iAbsoluteTeam =
        (Setback.numTeams - iDealerTeam) % Setback.numTeams
    score |> AbstractScore.shift iAbsoluteTeam

let mutable gamesWon = AbstractScore.zero

/// A game has finished.
let onGameFinish (dealer, gameScore) =
    let iTeam =
        gameScore
            |> shiftScore dealer
            |> BootstrapGameState.winningTeamOpt
            |> Option.get
    gamesWon <- gamesWon + AbstractScore.forTeam iTeam 1

[<EntryPoint>]
let main argv =
    session.GameFinishEvent.Add(onGameFinish)
    session.Run(10)
    printfn $"{gamesWon}"
    0
