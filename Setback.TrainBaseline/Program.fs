namespace Bernsrite.Setback.Cfrm.Train

open System

open Cfrm

open PlayingCards
open Setback
open Setback.Cfrm

module Program =

    open System.Diagnostics

    let createGame (rng : Random) =
        let dealer = Seat.South
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer
            |> BaselineGameState

    let minimize batchSize =
        let rng = Random(0)
        let initialBatch =
            CfrBatch.create 2 (fun _ ->
                createGame rng)
        let batchNums = Seq.initInfinite ((+) 1)
        let stopwatch = Stopwatch()
        printfn "Batch size: %d" batchSize
        printfn "Iteration,Payoff,Size,Time"
        (initialBatch, batchNums)
            ||> Seq.fold (fun inBatch batchNum ->
                stopwatch.Start()
                let outBatch =
                    inBatch
                        |> CounterFactualRegret.minimizeBatch batchSize
                outBatch.StrategyProfile.Save("Baseline.strategy")
                stopwatch.Stop()
                printfn "%d,%A,%d,%A"
                    (batchNum * batchSize)
                    outBatch.ExpectedGameValues.[1]
                    outBatch.InfoSetMap.Count
                    stopwatch.Elapsed
                stopwatch.Reset()
                outBatch)
            |> ignore

    [<EntryPoint>]
    let main argv =
        minimize 100000
        0
