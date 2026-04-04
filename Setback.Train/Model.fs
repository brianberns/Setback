namespace Setback.Train

open System
open System.IO
open System.Diagnostics

open TorchSharp
open type torch
open type torch.nn
open type torch.optim
open FSharp.Core.Operators   // reclaim "float32" and other F# operators

open Setback.Learn
open Setback.Model

module AdvantageModel =

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
    type private Batch = seq<SubBatch>

    /// Breaks the given samples into randomized batches.
    let private createBatches
        batchSize subBatchSize (store : AdvantageSampleShuffledStore) =
        assert(store.Count < Int32.MaxValue)
        Seq.init (int store.Count) (fun iSample ->
            store[iSample])
                |> Seq.chunkBySize subBatchSize                 // e.g. sub-batches of 10,000 rows each
                |> Seq.chunkBySize (batchSize / subBatchSize)   // e.g. each batch contains 500,000 / 10,000 = 50 sub-batches

    /// Trains the given model on the given sub-batch of
    /// data.
    let private trainSubBatch settings model samples
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

        loss.item<float32>()

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
                for samples in batch do
                    trainSubBatch settings model samples criterion
            |]

            // optimize
        use _ = optimizer.step()

        loss

    /// Trains the given model using the given samples.
    let train
        settings
        (store : AdvantageSampleShuffledStore)
        (model : AdvantageModel) =

            // prepare training data
        let batches =
            createBatches
                settings.TrainingBatchSize
                settings.TrainingSubBatchSize
                store

            // train model
        use optimizer =
            Adam(
                model.parameters(),
                settings.LearningRate)
        use criterion = MSELoss()
        model.train()
        for epoch = 1 to settings.NumTrainingEpochs do

                // train epoch
            let loss =
                Array.last [|
                    for iBatch, batch in Seq.indexed batches do
                        let stopwatch = Stopwatch.StartNew()
                        trainBatch
                            settings model batch criterion optimizer
                        let seconds =
                            float32 stopwatch.ElapsedMilliseconds / 1000f
                        if settings.Verbose then
                            printfn $"Trained epoch {epoch} in {seconds} seconds"
                        settings.Writer.add_scalar(
                            $"advantage loss/iter%03d{store.Iteration}/epoch%03d{epoch}",
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

        model.eval()
