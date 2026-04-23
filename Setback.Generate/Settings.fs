namespace Setback.Generate

open System.IO
open TorchSharp

/// Hyperparameters.
type Settings =
    {
        /// Total number of deals to create when generating sample
        /// data at the start of each iteration.
        NumDealsPerIteration : int

        /// Number of deals to create per batch when generating
        /// sample data at the start of each iteration. E.g. 8000
        /// deals at 200 deals/batch is 40 batches.
        DealBatchSize : int

        /// Number of information sets in an inference batch.
        InferenceBatchSize : int

        /// Branch rate of the move tree when generating sample data
        /// at the start of each iteration. Larger values generate
        /// more samples.
        /// https://chatgpt.com/c/67b26aab-6504-8000-ba0e-0ae3c8a614ff
        SampleBranchRate : float

        /// Input and output size of a hidden layer within the neural
        /// network.
        HiddenSize : int

        /// Number of hidden layers within the neural network.
        NumHiddenLayers : int

        /// Device to use for running models.
        Device : torch.Device

        /// Tensorboard writer.
        Writer : Modules.SummaryWriter

        /// Path to directory where models will be saved.
        ModelDirPath : string

        /// Verbose output?
        Verbose : bool
    }

module Settings =

    /// Creates default settings.
    let create writer =
        {
#if DEBUG
            NumDealsPerIteration = 50
#else
            NumDealsPerIteration = 20_000
#endif
            DealBatchSize = 25
            InferenceBatchSize = 25_000
            SampleBranchRate = 1.5
            HiddenSize = 1_200
            NumHiddenLayers = 5
            Device = torch.CUDA
            ModelDirPath = Path.Combine(".", "Models")
            Writer = writer
            Verbose = true
        }

    /// Writes settings to Tensorboard.
    let write settings =
        let writer = settings.Writer
        writer.add_text(
            $"settings/NumDealsPerIteration",
            string settings.NumDealsPerIteration, 0)
        writer.add_text(
            $"settings/DealBatchSize",
            string settings.DealBatchSize, 0)
        writer.add_text(
            $"settings/InferenceBatchSize",
            string settings.InferenceBatchSize, 0)
        writer.add_text(
            $"settings/SampleBranchRate",
            string settings.SampleBranchRate, 0)
        writer.add_text(
            $"settings/HiddenSize",
            string settings.HiddenSize, 0)
        writer.add_text(
            $"settings/NumHiddenLayers",
            string settings.NumHiddenLayers, 0)

        settings.ModelDirPath   // to-do: move this somewhere else?
            |> Directory.CreateDirectory
            |> ignore
