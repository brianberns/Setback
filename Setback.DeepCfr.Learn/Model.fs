namespace Setback.DeepCfr.Learn

open TorchSharp
open type torch
open type torch.nn
open type torch.optim
open FSharp.Core.Operators   // reclaim "float32" and other F# operators

open MathNet.Numerics.LinearAlgebra

open Setback.DeepCfr.Model

/// An observed advantage event.
type AdvantageSample =
    {
        /// Encoded info set.
        Encoding : Encoding

        /// Observed regrets.
        Regrets : Vector<float32>

        /// Weight of this sample, as determined by 1-based
        /// iteration number. Later iterations have more weight.
        Weight : float32
    }

module AdvantageSample =

    /// Creates an advantage sample.
    let create infoSet regrets iteration =
        assert(Vector.length regrets = Model.outputSize)
        assert(iteration > 0)
        assert(iteration <= settings.NumIterations)
        {
            Encoding = Encoding.encode infoSet
            Regrets = regrets
            Weight = float32 iteration |> sqrt
        }

module AdvantageModel =

    /// A chunk of training data that fits on the GPU.
    type private SubBatch =
        {
            /// Encoded input rows.
            Inputs : float32 array2d

            /// Target result rows.
            Targets : float32 array2d

            /// Weight of each row.
            Weights : float32 array2d
        }

    module private SubBatch =

        /// Creates a sub-batch.
        let create inputs targets weights =
            let subbatch =
                {
                    Inputs = array2D inputs
                    Targets = array2D targets
                    Weights = array2D weights
                }
            assert(
                [
                    subbatch.Inputs.GetLength(0)
                    subbatch.Targets.GetLength(0)
                    subbatch.Weights.GetLength(0)
                ]
                    |> Seq.distinct
                    |> Seq.length = 1)
            subbatch

    /// A batch of training data.
    type private Batch = SubBatch[]

    /// Breaks the given samples into batches.
    let private createBatches samples : Batch[] =
        samples
            |> Seq.toArray
            |> Array.randomShuffle
            |> Array.chunkBySize     // e.g. sub-batches of 10,000 rows each
                settings.AdvantageSubBatchSize
            |> Array.chunkBySize (   // e.g. each batch contains 500,000 / 10,000 = 50 sub-batches
                settings.AdvantageBatchSize
                    / settings.AdvantageSubBatchSize)
            |> Array.map (
                Array.map (fun samples ->
                    let inputs, targets, weights =
                        samples
                            |> Array.map (fun sample ->
                                Encoding.toFloat32 sample.Encoding,
                                sample.Regrets,
                                Seq.singleton sample.Weight)
                            |> Array.unzip3
                    SubBatch.create inputs targets weights))

    /// Trains the given model on the given sub-batch of
    /// data.
    let private trainSubBatch model subbatch
        (criterion : Loss<Tensor, Tensor, Tensor>) =

            // move to GPU
        use inputs =
            tensor(
                subbatch.Inputs,
                device = settings.Device)
        use targets =
            tensor(
                subbatch.Targets,
                device = settings.Device)
        use weights =
            tensor(
                subbatch.Weights,
                device = settings.Device)

            // forward pass
        use loss =

            use rawLoss =
                use outputs = inputs --> model
                use outputs' = weights * outputs
                use targets' = weights * targets
                criterion.forward(outputs', targets')

                // scale loss
            let scale =
                float32 (subbatch.Inputs.GetLength(0))
                    / float32 settings.AdvantageBatchSize
            rawLoss * scale

            // backward pass
        loss.backward()

        loss.item<float32>()

    /// Trains the given model on the given batch of data
    /// using gradient accumulation.
    /// https://chat.deepseek.com/a/chat/s/2f688262-70d6-4fb9-a05e-c230fa871f83
    let private trainBatch model (batch : Batch) criterion
        (optimizer : Optimizer) =

            // clear gradients
        optimizer.zero_grad()

            // train sub-batches
        let loss =
            Array.last [|
                for subbatch in batch do
                    trainSubBatch model subbatch criterion
            |]

            // optimize
        use _ = optimizer.step()

        loss

    /// Trains the given model using the given samples.
    let train iter samples (model : AdvantageModel) =

            // prepare training data
        let batches = createBatches samples

            // train model
        use optimizer =
            Adam(
                model.parameters(),
                settings.LearningRate)
        use criterion = MSELoss()
        model.train()
        for epoch = 1 to settings.NumAdvantageTrainEpochs do
            let loss =
                Array.last [|
                    for batch in batches do
                        trainBatch model batch criterion optimizer
                |]
            settings.Writer.add_scalar(
                $"advantage loss/iter%03d{iter}",
                loss, epoch)
        model.eval()