namespace Setback.PlayModel

open System
open System.IO
open System.Runtime
open System.Text

open Microsoft.Extensions.FileSystemGlobbing

open Setback
open Setback.Learn
open Setback.Model
open Setback.Train

module Program =

    let parse (argv : _[]) =
        if argv.Length = 0 then
            seq { "AdvantageModel.pt" }
        else
            let matcher = Matcher()
            for arg in argv do
                matcher.AddInclude(arg) |> ignore
            matcher.GetResultsInFullPath(".")

    /// Champion for comparison.
    let private createChampion settings =
        let model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                0.0,
                TorchSharp.torch.CPU)   // always run on CPU
        model.load("Champion.pt") |> ignore
        model.eval()
        Strategy.createPlayer model

    /// Plays models against champion.
    let run paths =

        let settings =
            let writer = TensorBoard.createWriter ()
            Settings.create writer
        Settings.write settings

        printfn $"Server garbage collection: {GCSettings.IsServerGC}"

        let champion = createChampion settings

        use model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                0.0,
                TorchSharp.torch.CPU)   // always run on CPU

        for path in paths do
            model.load(path : string) |> ignore
            model.eval()
            let player = Strategy.createPlayer model
            let nGames =
                Tournament.run settings.NumEvaluationGames champion player
            let payoff =
                float nGames / float settings.NumEvaluationGames
            printfn $"{Path.GetFileName(path)}: {payoff}"

    [<EntryPoint>]
    let main argv =
        Console.OutputEncoding <- Encoding.UTF8
        parse argv |> run
        0
