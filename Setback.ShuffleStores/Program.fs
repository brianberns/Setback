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

    let shuffle (group : AdvantageSampleStoreGroup) =

            // get every sample's index pair (optimized for space)
        let indexPairs =
            [|
                let storeCount = group.Count
                assert(storeCount <= int32 Int16.MaxValue)

                for iStore = 0s to int16 storeCount - 1s do

                    let sampleCount = group[int iStore].Count
                    assert(sampleCount <= int64 Int32.MaxValue)

                    for iSample = 0 to int sampleCount - 1 do
                        struct (iStore, iSample)
            |]

            // randomize samples
        let indexPairIndexes =
            Array.randomShuffle [| 0 .. indexPairs.Length - 1 |]
        seq {
            for iPair in indexPairIndexes do
                let struct (iStore, iSample) = indexPairs[iPair]
                group[int iStore][iSample]
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
        let samples = shuffle group

            // write to output
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
        AdvantageSampleShuffledStore.appendSamples
            samples shuffledStore

    [<EntryPoint>]
    let main argv =
        Console.OutputEncoding <- Encoding.UTF8
        parse argv |> run
        0
