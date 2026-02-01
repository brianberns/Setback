namespace Bernsrite.Setback.Cfrm.TrainBaseline

open System
open System.Diagnostics

open FastCfr

open MathNet.Numerics.LinearAlgebra

open PlayingCards
open Setback
open Setback.Cfrm

module InfoSetMap =

    /// Creates a strategy profile from the given info set map.
    let toStrategyProfile infoSetMap =
        infoSetMap
            |> Map.map (fun _ infoSet ->
                let strategy =
                    infoSet
                        |> InformationSet.getAverageStrategy
                        |> Vector.toArray
                assert(strategy.Length > 1)
                strategy)
            |> StrategyProfile

module Program =

    /// Initializes a game.
    let createGame (rng : Random) =
        let dealer = Seat.South
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer
            |> BaselineGameState.createGameState

    /// Generates an infinite sequence of games.
    let generate rng =
        Seq.initInfinite (fun _ ->
            createGame rng)

    let run () =

            // settings for this run
        let chunkSize = 100
        printfn $"Chunk size: {chunkSize}"

            // train on chunks of deals lazily
        let tuples =
            let rng = Random(0)
            generate rng
                |> Seq.chunkBySize chunkSize
                |> Trainer.trainScan Setback.numTeams

        printfn "Iteration, # Info Sets, Duration (ms), Saved"
        let stopwatch = Stopwatch.StartNew()
        for (iter, state) in Seq.indexed tuples do
            printf $"{iter}, {state.InfoSetMap.Count}, {stopwatch.ElapsedMilliseconds}"
            if iter % 10 = 0 then
                (state.InfoSetMap
                    |> InfoSetMap.toStrategyProfile)
                    .Save("Baseline.strategy")
                printfn ", saved"
            else
                printfn ""
            stopwatch.Restart()

    Console.OutputEncoding <- System.Text.Encoding.UTF8
    printfn $"Server garbage collection: {Runtime.GCSettings.IsServerGC}"
    run ()
