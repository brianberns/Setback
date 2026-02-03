namespace Setback.Cfrm.TrainBaseline

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

    /// Determines whether the given number is a power of two.
    let isPowerOfTwo n =
        n > 0 && (n &&& (n - 1)) = 0

    let run () =

            // settings for this run
        let chunkSize = 1
        printfn $"Chunk size: {chunkSize}"

            // train on chunks of deals lazily
        let states =
            let rng = Random(0)
            generate rng
                |> Seq.chunkBySize chunkSize
                |> Trainer.trainScan Setback.numTeams

        printfn "Chunk, Iteration, # Info Sets, Duration (ms), Saved"
        let stopwatch = Stopwatch.StartNew()
        for (iChunk, state) in Seq.indexed states do
            let chunkNum = iChunk + 1
            let save =
                let threshold = 1000
                if chunkNum < threshold then isPowerOfTwo chunkNum
                else chunkNum % threshold = 0
            if save then
                printf $"{chunkNum}, {chunkNum * chunkSize}, {state.InfoSetMap.Count}, {stopwatch.ElapsedMilliseconds}"
                (state.InfoSetMap
                    |> InfoSetMap.toStrategyProfile)
                    .Save("Baseline.strategy")
                printfn ", saved"
            stopwatch.Restart()

    Console.OutputEncoding <- System.Text.Encoding.UTF8
    printfn $"Server garbage collection: {Runtime.GCSettings.IsServerGC}"
    run ()
