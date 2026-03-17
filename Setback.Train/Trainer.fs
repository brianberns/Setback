namespace Setback.Train

open System.Diagnostics

open Setback
open Setback.Learn
open Setback.Model

module Trainer =

    /// CFR champion.
    let private champion =
        Cfrm.PlaySelf.Program.getPlayer "Champion.db"

    /// Evaluates the given model by playing it against a
    /// standard.
    let private evaluate settings iteration model =

        let nGames =
            Tournament.run
                false             // avoid cross-thread TorchSharp problems (memory leaks, toFloat crash)
                settings.NumEvaluationDeals
                champion
                (Strategy.createPlayer model)
        let payoff =
            float32 nGames / float32 settings.NumEvaluationDeals

        if settings.Verbose then
            printfn $"Tournament payoff: %0.5f{payoff}"
        settings.Writer.add_scalar(
            $"advantage tournament", payoff, iteration)

    /// Uses stored samples to train a new model.
    let trainModel settings (sampleStores : AdvantageSampleStoreGroup) =

            // train new model
        let stopwatch = Stopwatch.StartNew()
        use model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                settings.DropoutRate,
                settings.Device)
        AdvantageModel.train settings sampleStores model
        stopwatch.Stop()

           // save the model
        if settings.Verbose then
            printfn $"Trained model on {sampleStores.NumSamples} samples in {stopwatch.Elapsed} \
                (%.2f{float stopwatch.ElapsedMilliseconds / float sampleStores.NumSamples} ms/sample)"

            // evaluate model
        evaluate settings sampleStores.Iteration model
