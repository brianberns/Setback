namespace Setback.Model

open TorchSharp
open type torch
open type torch.nn

open Setback
open PlayingCards

/// Neural network that maps an input tensor to an output
/// tensor.
type Model = Module<Tensor, Tensor>

module Model =

    /// Size of neural network input.
    let inputSize = Encoding.encodedLength

    /// Size of neural network output.
    let outputSize = Bid.numBids + Card.numCards

/// Skip connection module.
type SkipConnection(inner : Model) as this =
    inherit Model($"{inner.GetName()}Skip")

    do this.register_module("inner", inner)

    override _.forward(input) =
        use x = input --> inner
        x + input

/// Model used for learning advantages.
type AdvantageModel(
    hiddenSize : int,
    numHiddenLayers : int,
    dropoutRate : float,
    device : torch.Device) as this =
    inherit Model("AdvantageModel")

    /// Skip connection DSL support.
    let SkipConnection(inner) = new SkipConnection(inner)

    /// Model layers.
    let sequential =
        Sequential [|

                // input layer
            Linear(
                Model.inputSize,
                hiddenSize) :> Model
            ReLU()
            Dropout(dropoutRate)

                // hidden layers
            for _ = 1 to numHiddenLayers do
                SkipConnection(
                    Sequential(
                        Linear(
                            hiddenSize,
                            hiddenSize),
                        ReLU(),
                        Dropout(dropoutRate)))

                // output layer
            Linear(
                hiddenSize,
                Model.outputSize)
        |]

    do
        this.RegisterComponents()
        this.``to``(device) |> ignore

    /// Copies the given model onto the given device.
    static member Copy(source : AdvantageModel, device) =
        let model =
            new AdvantageModel(
                source.HiddenSize,
                source.NumHiddenLayers,
                source.DropoutRate,
                device)
        model.load_state_dict(source.state_dict()) |> ignore
        model

    /// Hidden layer size.
    member _.HiddenSize = hiddenSize

    /// Number of hidden layers.
    member _.NumHiddenLayers = numHiddenLayers

    /// Dropout rate.
    member _.DropoutRate = dropoutRate

    /// Device on which this model resides.
    member _.Device = device

    /// Runs the model on the given tensor.
    override _.forward(input) =
        sequential.forward(input)

module AdvantageModel =

    /// Gets advantages for the given info sets.
    let getAdvantages infoSets (model : AdvantageModel) =
        assert(not model.training)
        use _ = torch.no_grad()
        use input =
            let encoded =
                infoSets
                    |> Array.map (
                        Encoding.encode
                            >> Encoding.toFloat32Array)
                    |> array2D
            tensor(encoded, device = model.Device)
        input --> model
