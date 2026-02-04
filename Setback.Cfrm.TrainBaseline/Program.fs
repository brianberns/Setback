namespace Bernsrite.Setback.Cfrm.TrainBaseline

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
                CfrBatch.load batchFileName (fun _ -> createGame Random.Shared)
            else
                let numPlayers = 2
                CfrBatch.create numPlayers (fun _ -> createGame Random.Shared)
        let batchNums = Seq.initInfinite ((+) 1)   // 1, 2, 3, ...
        let stopwatch = Stopwatch()

            // run CFR
        printfn "Iteration,Payoff,Size,Time"
        (initialState, batchNums)
            ||> Seq.fold (fun inBatch _ ->

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
                    outBatch.NumIterations
                    outBatch.ExpectedGameValues[1]   // value of a deal from the first bidder's point of view
                    outBatch.InfoSetMap.Count
                    stopwatch.Elapsed
                stopwatch.Reset()

                    // feed results into next loop
                outBatch)
            |> ignore

    minimize 100_000
