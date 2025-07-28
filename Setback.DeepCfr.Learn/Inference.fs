namespace Setback.DeepCfr.Learn

open Setback.DeepCfr.Model

module Array =

    module Parallel =

        /// Maps the given arrays using the given function .
        let map2 mapping array1 array2 =
            Array.zip array1 array2
                |> Array.Parallel.map (fun (val1, val2) ->
                    mapping val1 val2)

module Inference =

    /// Gets strategies for the given batch of info sets.
    let private getStrategies infoSets modelOpt =
        match modelOpt with

                // batch inference
            | Some (model : AdvantageModel) ->
                infoSets
                    |> Array.chunkBySize settings.AdvantageSubBatchSize
                    |> Array.collect (
                        Strategy.getFromAdvantage model)

                // no model yet, random strategies
            | None ->
                infoSets
                    |> Array.map (fun infoSet ->
                        Strategy.random infoSet.LegalPlays.Length)

    /// Replaces items in the given arrays.
    let private replace chooser fromItems (toItems : _[]) =
        let result, n =
            (0, fromItems)
                ||> Array.mapFold (
                    Array.mapFold (fun i fromItem ->
                        match chooser fromItem with
                            | Some toItem -> toItem, i
                            | None ->
                                toItems[i], i + 1))
        assert(n = toItems.Length)
        result

    /// Replaces all GetStragey nodes in a level.
    let private replaceGetStrategy modelOpt nodeArrays =

            // create non-GS nodes from GS nodes in one batch
        let batch =
            let infoSets, conts =
                nodeArrays
                    |> Seq.concat
                    |> Seq.choose (function
                        | GetStrategy gs ->
                            Some (gs.InformationSet, gs.Continuation)
                        | _ -> None)
                    |> Seq.toArray
                    |> Array.unzip
            (getStrategies infoSets modelOpt, conts)
                ||> Array.Parallel.map2 (|>)
        assert(batch |> Seq.forall (_.IsGetStrategy >> not))

            // replace GS nodes in the correct order
        (nodeArrays, batch)
            ||> replace (function
                | GetStrategy _ -> None
                | node -> Some node)

    /// Extracts details from the given complete node.
    let private getComp = function
        | Complete comp -> comp
        | _ -> failwith "Unexpected"

    /// Extract samples from the given complete node.
    let rec private getSamples comp =
        [|
            match comp.SampleOpt with
                | Some sample -> sample
                | None -> ()
            for child in comp.Children do
                yield! getSamples child
        |]

    /// Recursively drives the given nodes to completion.
    let complete modelOpt (nodes : Node[]) =

        let rec loop depth nodeArrays =
            if Array.isEmpty nodeArrays then
                Array.empty
            else
                    // replace initial nodes
                let nonInitialArrays =
                    replaceGetStrategy modelOpt nodeArrays

                    // drive children of in-progress nodes to completion
                let comps =
                    let childArrays, conts =
                        nonInitialArrays
                            |> Seq.concat
                            |> Seq.choose (function
                                | GetStrategy _ -> failwith "Unexpected"
                                | GetUtility gu ->
                                    Some (gu.Children, gu.Continuation)
                                | _ -> None)
                            |> Seq.toArray
                            |> Array.unzip
                    let comps =
                        loop (depth + 1) childArrays
                            |> Array.map (Array.map getComp)
                    (comps, conts)
                        ||> Array.Parallel.map2 (|>)

                    // replace in-progress nodes
                (nonInitialArrays, comps)
                    ||> replace (function
                        | GetStrategy _ -> failwith "Unexpected"
                        | GetUtility _ -> None
                        | Complete _ as node -> Some node)

        loop 0 [| nodes |]
            |> Array.exactlyOne
            |> Array.collect (getComp >> getSamples)
