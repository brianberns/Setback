namespace Setback.PlayKiller

open TorchSharp
open Setback.Model

/// Hyperparameters.
type Settings =
    {
        /// Input and output size of a hidden layer within the neural
        /// network.
        HiddenSize : int

        /// Number of hidden layers within the neural network.
        NumHiddenLayers : int

        /// Device to use for running models.
        Device : torch.Device
    }

module Settings =

    /// Creates default settings.
    let create () =
        {
            HiddenSize = Encoding.encodedLength
            NumHiddenLayers = 4
            Device = torch.CPU
        }
