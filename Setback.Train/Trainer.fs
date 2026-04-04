namespace Setback.Train

open System.Diagnostics

open Setback
open Setback.Learn
open Setback.Model

module Trainer =

    /// CFR champion.
    let private champion =
        Cfrm.DatabasePlayer.player "Champion.db"
            |> Cfrm.PlaySelf.Program.getPlayer

    /// Evaluates the given model by playing it against a
    /// standard.
    let private evaluate settings iteration model =

        let nGames =
            Tournament.run
                false             // avoid cross-thread TorchSharp problems (memory leaks, toFloat crash)
                settings.NumEvaluationGames
                champion
                (Strategy.createPlayer model)
        let payoff =
            float32 nGames / float32 settings.NumEvaluationGames

        if settings.Verbose then
            printfn $"Tournament payoff: %0.5f{payoff}"
        settings.Writer.add_scalar(
            $"advantage tournament", payoff, iteration)

    /// Uses stored samples to train a new model.
    let trainModel settings store =

            // train new model
        let stopwatch = Stopwatch.StartNew()
        use model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                settings.DropoutRate,
                settings.Device)
        AdvantageModel.train settings store model
        stopwatch.Stop()

           // save the model
        if settings.Verbose then
            printfn $"Trained model on {store.Count} samples in {stopwatch.Elapsed} \
                (%.2f{float stopwatch.ElapsedMilliseconds / float store.Count} ms/sample)"

            // evaluate model
        evaluate settings store.Iteration model
