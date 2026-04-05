namespace Setback.ShuffleStores

namespace Setback.PlayModel

open System
open System.IO
open System.Text

open Microsoft.Extensions.FileSystemGlobbing

open Setback.Learn

module Program =

    let parse (argv : _[]) =
        let matcher = Matcher()
        for arg in argv do
            matcher.AddInclude(arg) |> ignore
        matcher.GetResultsInFullPath(".")

    let cleanup tempStores =
        for (tempStore : AdvantageSampleShuffledStore) in tempStores do
            let path = tempStore.Path
            tempStore.Dispose()
            File.Delete(path)

    let distribute (rng : Random) (group : AdvantageSampleStoreGroup) =
        let tempStores =
            let nTempStores = 64
            Array.init nTempStores (fun iTempStore ->
                AdvantageSampleShuffledStore.create
                    group.Iteration
                    $"AdvantageSamples-i%03d{group.Iteration}-temp%02d{iTempStore}.sbin")
        try

            for inputStore in group.Stores do
                assert(inputStore.Count < Int32.MaxValue)
                for sample in AdvantageSampleStore.readSamples inputStore do
                    tempStores
                        |> Array.randomChoiceWith rng
                        |> AdvantageSampleShuffledStore.writeSamples [sample]

            [|
                for tempStore in tempStores do
                    let path = tempStore.Path
                    tempStore.Dispose()
                    AdvantageSampleShuffledStore.openRead path   // re-open for read
            |]

        with _ ->
            cleanup tempStores
            reraise ()

    let collect rng (tempStores : AdvantageSampleShuffledStore[]) =
        seq {
            for tempStore in tempStores do
                assert(tempStore.Count < Int32.MaxValue)
                let samples =
                    AdvantageSampleShuffledStore.readSamples tempStore
                        |> Seq.toArray
                Array.randomShuffleInPlaceWith rng samples
                yield! samples
        }

    let run paths =

            // open inputs
        let sampleStores =
            paths
                |> Seq.map AdvantageSampleStore.openRead
                |> Seq.toArray
        printfn "Sample stores:"
        for store in sampleStores do
            printfn $"   {Path.GetFileName(store.Path)}: {store.Count} samples"
        let group = { Stores = sampleStores }

            // shuffle samples
        let rng = Random()
        let tempStores = distribute rng group
        let samples = collect rng tempStores

            // write to output
        try
            use shuffledStore =
                let path =
                    let unique =
                        let timespan = DateTime.Now - DateTime.Today
                        int timespan.TotalSeconds
                    $"AdvantageSamples-i%03d{group.Iteration}-%05d{unique}.sbin"
                printfn $"Creating shuffled sample store: {path}"
                AdvantageSampleShuffledStore.create
                    group.Iteration
                    path
            AdvantageSampleShuffledStore.writeSamples
                samples shuffledStore
        finally
            cleanup tempStores

    [<EntryPoint>]
    let main argv =
        Console.OutputEncoding <- Encoding.UTF8
        parse argv |> run
        0
