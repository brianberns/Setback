namespace Setback.Train

open System
open System.IO
open System.Runtime
open System.Text

open Setback.Learn
open Setback.Model

module Program =

    let run path =

            // get settings
        let settings =
            let writer = TensorBoard.createWriter ()
            Settings.create writer
        Settings.write settings
        if settings.Verbose then
            printfn "Settings:"
            printfn $"   Server garbage collection: {GCSettings.IsServerGC}"
            printfn $"   Hidden size: {settings.HiddenSize}"
            printfn $"   # hidden layers: {settings.NumHiddenLayers}"
            printfn $"   # training epochs: {settings.NumTrainingEpochs}"
            printfn $"   Training batch size: {settings.TrainingBatchSize}"
            printfn $"   Training sub-batch size: {settings.TrainingSubBatchSize}"
            printfn $"   Dropout rate: {settings.DropoutRate}"
            printfn $"   Learning rate: {settings.LearningRate}"
            printfn $"   # evaluation games: {settings.NumEvaluationGames}"
            printfn $"   Device: {settings.Device}"
            printfn $"   Model directory: {settings.ModelDirPath}"
            printfn $"   Model input size: {Model.inputSize}"
            printfn $"   Model output size: {Model.outputSize}"

            // get training data
        let store = AdvantageSampleShuffledStore.openRead path
        if settings.Verbose then
            printfn "Sample store:"
            printfn $"   {Path.GetFileName(store.Path)}: {store.Count} samples"

            // train model
        Trainer.trainModel settings store

    [<EntryPoint>]
    let main argv =
        Console.OutputEncoding <- Encoding.UTF8
        if argv.Length <> 1 then failwith "Invalid arguments"
        else run argv[1]
        0
