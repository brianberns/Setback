namespace Setback.DeepCfr.Learn

open System
open System.Diagnostics
open System.IO

open Setback
open Setback.DeepCfr.Model

/// Advantage state.
type AdvantageState =
    {
        /// Current model.
        ModelOpt : Option<AdvantageModel>

        /// Reservoir of training data.
        Reservoir : Reservoir<AdvantageSample>
    }

    interface IDisposable with

        /// Cleanup.
        member this.Dispose() =
            this.ModelOpt
                |> Option.iter _.Dispose()

module AdvantageState =

    /// Creates an initial advantage state.
    let create rng =
        {
            ModelOpt = None
            Reservoir =
                Reservoir.create rng
                    settings.NumAdvantageSamples
        }

module Trainer =

    /// Traverses one deal.
    let private traverse iter deal =
        let rng = Random()   // each thread has its own RNG
        Traverse.traverse iter deal rng

    /// Generates training data using the given model.
    let private generateSamples iter modelOpt =

        settings.Writer.add_scalar(
            $"advantage samples/iter%03d{iter}",
            0f, 0)

        let chunkSize = settings.TraversalBatchSize
        let rng = Random ()
        Array.zeroCreate<int> settings.NumTraversals
            |> Array.chunkBySize chunkSize
            |> Array.indexed
            |> Array.collect (fun (i, chunk) ->

                let samples =
                    OpenDeal.generate
                        rng chunk.Length (traverse iter)
                        |> Inference.complete modelOpt
                GC.Collect()   // clean up continuations

                settings.Writer.add_scalar(
                    $"advantage samples/iter%03d{iter}",
                    float32 samples.Length / float32 chunkSize,
                    (i + 1) * chunkSize)

                samples)

    /// Adds the given samples to the given reservoir and then
    /// uses the reservoir to train a new model.
    let private trainAdvantageModel iter samples state =

            // cache new training data
        let resv =
            Reservoir.addMany samples state.Reservoir

            // train new model
        let stopwatch = Stopwatch.StartNew()
        let model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                settings.Device)
        AdvantageModel.train iter resv.Items model
        stopwatch.Stop()
        if settings.Verbose then
            printfn $"Trained model on {resv.Items.Count} samples in {stopwatch.Elapsed} \
                (%.2f{float stopwatch.ElapsedMilliseconds / float resv.Items.Count} ms/sample)"

        {
            Reservoir = resv
            ModelOpt = Some model
        }

    /// Trains a new model using the given model.
    let private updateModel iter state =

            // generate training data from existing model
        let stopwatch = Stopwatch.StartNew()
        let samples =
            generateSamples iter state.ModelOpt
        if settings.Verbose then
            printfn $"\n{samples.Length} samples generated in {stopwatch.Elapsed}"

            // train a new model on GPU
        let state =
            trainAdvantageModel iter samples state
        state.ModelOpt
            |> Option.iter (fun model ->
                Path.Combine(
                    settings.ModelDirPath,
                    $"AdvantageModel%03d{iter}.pt")
                        |> model.save
                        |> ignore)
        settings.Writer.add_scalar(
            $"advantage reservoir",
            float32 state.Reservoir.Items.Count,
            iter)

        state

    /// Evaluates the given model by playing it against a
    /// standard.
    let private evaluate iter (model : AdvantageModel) =
        let avgPayoff =
            Tournament.run
                (Random(0))       // use repeatable test set, not seen during training
                Trickster.player
                (Strategy.createPlayer model)
        settings.Writer.add_scalar(
            $"advantage tournament", avgPayoff, iter)

    /// Trains a single iteration.
    let private trainIteration iter state =
        if settings.Verbose then
            printfn $"\n*** Iteration {iter} ***"
        let state = updateModel iter state
        state.ModelOpt
            |> Option.iter (evaluate iter)
        state

    /// Trains for the given number of iterations.
    let train () =

        if settings.Verbose then
            printfn $"Model input size: {Model.inputSize}"
            printfn $"Model output size: {Model.outputSize}"

            // run the iterations
        let state = AdvantageState.create (Random())
        let iterNums = seq { 1 .. settings.NumIterations }
        (state, iterNums)
            ||> Seq.fold (fun state iter ->
                trainIteration iter state)
