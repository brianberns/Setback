namespace Setback.Generate

open System
open System.Diagnostics
open System.IO
open System.Runtime
open System.Text

open Setback
open Setback.Learn
open Setback.Model

module Program =

    /// Generates training data for the given iteration.
    let private generateSamples settings iteration state =

        /// TensorBoard logging.
        let log value step =
            settings.Writer.add_scalar(
                $"advantage samples/iter%03d{iteration}",
                value, step)

            // start TensorBoard y-axis at 0
        log 0f 0

            // divide games for this iteration into batches,
            // including possible runt batch at the end
        let batchSizes =
            Seq.replicate settings.NumGamesPerIteration ()
                |> Seq.chunkBySize settings.GameBatchSize
                |> Seq.map _.Length

            // generate samples for each batch
        Array.sum [|
            for iBatch, numGames in Seq.indexed batchSizes do
                assert(numGames <= settings.GameBatchSize)

                    // generate samples
                let samples =
                    Game.playGames Random.Shared true numGames
                        (Traverse.traverse settings iteration)
                        |> Inference.complete
                            settings.InferenceBatchSize
                            state.ModelOpt

                    // save samples
                AdvantageSampleStore.appendSamples
                    samples state.SampleStore
                log
                    (float32 samples.Length / float32 numGames)    // average number of generated samples per game in this batch
                    (iBatch * settings.GameBatchSize + numGames)   // total number of games so far

                samples.Length
        |]

    /// Parses command line arguments.
    let private parse (argv : string[]) =
        match argv.Length with
            | 0 -> None, 0
            | 1 ->
                let modelPath = argv[0]
                let iter =
                    let chunks =
                        Path.GetFileNameWithoutExtension(modelPath)   // e.g. "AdvantageModel-i001.pt"
                            .Split('-')
                    Int32.Parse(chunks[1][1 ..])
                Some modelPath, iter
            | _ -> failwith $"Invalid arguments: {argv}"

    /// Generates samples for the given iteration using the given model.
    let private run modelPathOpt iteration =

            // get settings
        let settings =
            let writer = TensorBoard.createWriter ()
            Settings.create writer
        Settings.write settings
        if settings.Verbose then
            printfn "Settings:"
            printfn $"   Server garbage collection: {GCSettings.IsServerGC}"
            printfn $"   Iteration: {iteration}"
            printfn $"   # games to generate: {settings.NumGamesPerIteration}"
            printfn $"   Game batch size: {settings.GameBatchSize}"
            printfn $"   Inference batch size: {settings.InferenceBatchSize}"
            printfn $"   Sample branch rate: {settings.SampleBranchRate}"
            printfn $"   Hidden size: {settings.HiddenSize}"
            printfn $"   # hidden layers: {settings.NumHiddenLayers}"
            printfn $"   Device: {settings.Device}"
            printfn $"   Model directory: {settings.ModelDirPath}"
            printfn $"   Model input size: {Model.inputSize}"
            printfn $"   Model output size: {Model.outputSize}"

            // initialize model, if specified
        let modelOpt =
            modelPathOpt
                |> Option.map (fun modelPath ->
                    let model =
                        new AdvantageModel(
                            settings.HiddenSize,
                            settings.NumHiddenLayers,
                            0.0,   // dropout not used during inference
                            settings.Device)
                    if settings.Verbose then
                        printfn $"Loading model from {modelPath}"
                    model.load(modelPath : string) |> ignore
                    model.eval()
                    model)

            // initialize state
        use state =
            let unique =
                let timespan = DateTime.Now - DateTime.Today
                int timespan.TotalSeconds
            let path =
                Path.Combine(
                    settings.ModelDirPath,
                    $"AdvantageSamples-i%03d{iteration}-%05d{unique}.bin")
            if settings.Verbose then
                printfn $"Creating sample store: {path}"
            path
                |> AdvantageSampleStore.create iteration
                |> AdvantageState.create modelOpt

            // generate samples
        let stopwatch = Stopwatch.StartNew()
        let numSamples = generateSamples settings iteration state
        if settings.Verbose then
            printfn $"{numSamples} samples generated in {stopwatch.Elapsed}"

    /// Generates samples for the next iteration.
    [<EntryPoint>]
    let main argv =
        Console.OutputEncoding <- Encoding.UTF8
        let modelPathOpt, iter = parse argv
        run modelPathOpt (iter + 1)
        0
