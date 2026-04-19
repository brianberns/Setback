namespace Setback.Train

open System
open System.IO

open TorchSharp
open type torch
open type torch.nn
open type torch.optim
open FSharp.Core.Operators   // reclaim "float32" and other F# operators

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
    let private evaluate settings iteration epoch model =

            // determine payoff
        let nGames =
            use model = AdvantageModel.Copy(model, CPU)   // avoid cross-thread TorchSharp GPU problems (memory leaks, toFloat crash)
            model.eval()
            Tournament.run
                settings.NumEvaluationGames
                champion
                (Strategy.createPlayer model)
        let payoff =
            float32 nGames / float32 settings.NumEvaluationGames

            // write payoff
        if settings.Verbose then
            printfn $"Epoch {epoch} tournament payoff: %0.5f{payoff}"
        settings.Writer.add_scalar(
            $"advantage tournament/iter%03d{iteration}", payoff, epoch)

    /// A chunk of training data that fits on the GPU and in
    /// memory.
    type private SubBatch = AdvantageSample[]

    module private SubBatch =

        /// Extracts inputs from a sub-batch.
        let private toInputs (samples : SubBatch) =
            samples
                |> Array.map (
                    _.Encoding
                        >> Encoding.toFloat32Array)
                |> array2D

        /// Extracts targets from a sub-batch.
        let private toTargets (samples : SubBatch) =
            samples
                |> Array.map _.Regrets
                |> array2D

        /// Extracts weights from a sub-batch.
        let private toWeights (samples : SubBatch) =
            samples
                |> Array.map (
                    _.Iteration
                        >> float32
                        >> sqrt
                        >> Array.singleton)
                |> array2D

        /// Extracts 2D arrays from a sub-batch.
        let to2dArrays samples =
            let arrays =
                Array.Parallel.map (fun f -> f samples)
                    [| toInputs; toTargets; toWeights |]
            let inputs = arrays[0]
            let targets = arrays[1]
            let weights = arrays[2]
            assert(
                [
                    inputs.GetLength(0)
                    targets.GetLength(0)
                    weights.GetLength(0)
                ]
                    |> Seq.distinct
                    |> Seq.length = 1)
            assert(weights.GetLength(1) = 1)
            inputs, targets, weights

    /// A logical batch of training data that might be too
    /// large to fit on the GPU or in memory.
    type private Batch = SubBatch[]

    /// Breaks the given samples into randomized batches.
    let private createBatches batchSize subBatchSize store : seq<Batch> =
        AdvantageSampleStore.readSamples store
            |> Seq.chunkBySize subBatchSize                 // e.g. sub-batches of 10,000 rows each
            |> Seq.chunkBySize (batchSize / subBatchSize)   // e.g. each batch contains 500,000 / 10,000 = 50 sub-batches

    /// Trains the given model on the given sub-batch of
    /// data.
    let private trainSubBatch settings needLoss model samples
        (criterion : Loss<Tensor, Tensor, Tensor>) =

            // move to GPU
        let inputs2d, targets2d, weights2d =
            SubBatch.to2dArrays samples
        use inputs = tensor(inputs2d, device = settings.Device)
        use targets = tensor(targets2d, device = settings.Device)
        use weights = tensor(weights2d, device = settings.Device)

            // forward pass
        use loss =

                // compute loss for this sub-batch
            use rawLoss =
                use outputs = inputs --> model
                use outputs' = weights * outputs
                use targets' = weights * targets
                criterion.forward(outputs', targets')

                // scale loss to batch size
            let scale =
                float32 (inputs2d.GetLength(0))
                    / float32 settings.TrainingBatchSize
            rawLoss * scale

            // backward pass
        loss.backward()

            // need loss value?
        if needLoss then
            Some (loss.item<float32>())
        else None

    /// Trains the given model on the given batch of data
    /// using gradient accumulation.
    /// https://chat.deepseek.com/a/chat/s/2f688262-70d6-4fb9-a05e-c230fa871f83
    let private trainBatch settings model (batch : Batch) criterion
        (optimizer : Optimizer) =

            // clear gradients
        optimizer.zero_grad()

            // train sub-batches
        let loss =
            Array.last [|
                for iBatch = 0 to batch.Length - 1 do
                    let samples = batch[iBatch]
                    let needLoss = (iBatch = batch.Length - 1)
                    trainSubBatch
                        settings needLoss model samples criterion
            |] |> Option.get

            // optimize
        use _ = optimizer.step()

        loss

    /// Instruments the given sequence for timing.
    let private timed (source : seq<_>) =
        seq {
            use e = source.GetEnumerator()
            let mutable hasNext = true
            while hasNext do
                let sw = System.Diagnostics.Stopwatch.StartNew()
                hasNext <- e.MoveNext()
                if hasNext then
                    yield e.Current, sw
        }

    /// Trains the given model using the given samples.
    let train settings (store : AdvantageSampleStore) =

            // create model
        use model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                settings.DropoutRate,
                settings.Device)

            // train model
        use optimizer =
            Adam(
                model.parameters(),
                settings.LearningRate)
        use criterion = MSELoss()
        for epoch = 1 to settings.NumTrainingEpochs do

                // prepare training data
            let batches =
                let store =   // reset for each epoch
                    AdvantageSampleStore.openRead store.Path
                createBatches
                    settings.TrainingBatchSize
                    settings.TrainingSubBatchSize
                    store

                // train epoch
            let loss =
                let tuples = Seq.indexed (timed batches)
                Array.last [|
                    for iBatch, (batch, stopwatch) in tuples do
                        trainBatch
                            settings model batch criterion optimizer
                        let seconds =
                            float32 stopwatch.ElapsedMilliseconds / 1000f
                        settings.Writer.add_scalar(
                            $"training time/iter%03d{store.Iteration}/epoch%03d{epoch}",
                            seconds, iBatch)
                |]
            settings.Writer.add_scalar(
                $"advantage loss/iter%03d{store.Iteration}",
                loss, epoch)

                // save model
            let path =
                Path.Combine(
                    settings.ModelDirPath,
                    $"AdvantageModel-i%03d{store.Iteration}-e%03d{epoch}.pt")
            model.save(path) |> ignore

                // evaluate model
            evaluate settings store.Iteration epoch model
