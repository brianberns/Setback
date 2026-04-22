namespace Setback.Web

open System
open System.IO

open Fable.Remoting.Server
open Fable.Remoting.Suave

module AdvantageModel =

    open Setback.Model

    /// Server is inference-only, so disable all gradient
    /// calculations.
    let private _noGrade = TorchSharp.torch.no_grad()

    /// Connects to Hearts model.
    let private connect dir =
        let model =
            new AdvantageModel(
                hiddenSize = Encoding.encodedLength * 2,
                numHiddenLayers = 4,
                dropoutRate = 0.0,
                device = TorchSharp.torch.CPU)
        let path = Path.Combine(dir, "AdvantageModel.pt")
        model.load(path) |> ignore
        model.eval()
        model

    /// Hearts API.
    let heartsApi dir =
        let rng = Random(0)
        let model = connect dir
        model.eval()
        {
            GetActionIndex =
                fun infoSet ->
                    async {
                        let strategy =
                            Strategy.getFromAdvantage
                                model
                                [|infoSet|]
                                |> Array.exactlyOne
                        return Vector.sample rng strategy
                    }
            GetStrategy =
                fun infoSet ->
                    async {
                        let strategy =
                            Strategy.getFromAdvantage
                                model
                                [|infoSet|]
                                |> Array.exactlyOne
                        return strategy.ToArray()
                            |> Array.map float
                    }
        }

module Remoting =

    /// Build API.
    let webPart dir =
        Remoting.createApi()
            |> Remoting.fromValue (AdvantageModel.heartsApi dir)
            |> Remoting.buildWebPart
