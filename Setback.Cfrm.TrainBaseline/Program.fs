﻿namespace Bernsrite.Setback.Cfrm.TrainBaseline

open System
open System.Diagnostics
open System.IO

open Cfrm

open PlayingCards
open Setback
open Setback.Cfrm

module Program =

    /// Initializes a game.
    let createGame (rng : Random) =
        let dealer = Seat.South
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer
            |> BaselineGameState

    /// Runs CFR with the given batch size.
    let minimize batchSize =

            // initialize
        let batchFileName = "Baseline.batch"
        let initialState =
            let fileInfo = FileInfo(batchFileName)
            if fileInfo.Exists then
                printfn "Loading existing file"
                let rng = Random(int fileInfo.Length)   // avoid revisiting original deals
                CfrBatch.load batchFileName (fun _ -> createGame rng)
            else
                let numPlayers = 2
                let rng = Random(0)
                CfrBatch.create numPlayers (fun _ -> createGame rng)
        let batchNums = Seq.initInfinite ((+) 1)   // 1, 2, 3, ...
        let stopwatch = Stopwatch()

            // run CFR
        printfn "Iteration,Payoff,Size,Time"
        (initialState, batchNums)
            ||> Seq.fold (fun inBatch batchNum ->

                    // run CFR on this batch of games
                stopwatch.Start()
                let outBatch =
                    inBatch
                        |> CounterFactualRegret.minimizeBatch batchSize
                outBatch.StrategyProfile.Save("Baseline.strategy")
                outBatch |> CfrBatch.save batchFileName
                stopwatch.Stop()

                    // report results from this batch
                printfn "%d,%A,%d,%A"
                    (batchNum * batchSize)
                    outBatch.ExpectedGameValues[1]   // value of a deal from the first bidder's point of view
                    outBatch.InfoSetMap.Count
                    stopwatch.Elapsed
                stopwatch.Reset()

                    // feed results into next loop
                outBatch)
            |> ignore

    [<EntryPoint>]
    let main argv =
        minimize 100000
        0
