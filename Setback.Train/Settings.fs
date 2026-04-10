namespace Setback.Train

open System.IO
open TorchSharp
open Setback.Model

/// Hyperparameters.
type Settings =
    {
        /// Input and output size of a hidden layer in the neural
        /// network.
        HiddenSize : int

        /// Number of hidden layers within the neural network.
        NumHiddenLayers : int

        /// Number of training epochs per iteration.
        NumTrainingEpochs : int

        /// Number of samples per logical training batch.
        TrainingBatchSize : int

        /// Number of samples per physical training sub-batch.
        TrainingSubBatchSize : int

        /// Dropout rate to use when training the model.
        DropoutRate : float

        /// Optimizer learning rate to use when training the model.
        LearningRate : float

        /// Number of games to evaluate model after training.
        NumEvaluationGames : int

        /// Device to use for training and running models.
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
            HiddenSize = Encoding.encodedLength
            NumHiddenLayers = 4
            NumTrainingEpochs = 100
            TrainingBatchSize = 100_000
            TrainingSubBatchSize = 25_000
            DropoutRate = 0.3
            LearningRate = 1e-3
#if DEBUG
            NumEvaluationGames = 40
#else
            NumEvaluationGames = 20000
#endif
            Device = torch.CUDA
            ModelDirPath = Path.Combine(".", "Models")
            Writer = writer
            Verbose = true
        }

    /// Writes settings to Tensorboard.
    let write settings =
        let writer = settings.Writer
        writer.add_text(
            $"settings/HiddenSize",
            string settings.HiddenSize, 0)
        writer.add_text(
            $"settings/NumHiddenLayers",
            string settings.NumHiddenLayers, 0)
        writer.add_text(
            $"settings/NumTrainingEpochs",
            string settings.NumTrainingEpochs, 0)
        writer.add_text(
            $"settings/TrainingBatchSize",
            string settings.TrainingBatchSize, 0)
        writer.add_text(
            $"settings/TrainingSubBatchSize",
            string settings.TrainingSubBatchSize, 0)
        writer.add_text(
            $"settings/DropoutRate",
            string settings.DropoutRate, 0)
        writer.add_text(
            $"settings/LearningRate",
            string settings.LearningRate, 0)
        writer.add_text(
            $"settings/NumEvaluationGames",
            string settings.NumEvaluationGames, 0)

        settings.ModelDirPath   // to-do: move this somewhere else?
            |> Directory.CreateDirectory
            |> ignore
