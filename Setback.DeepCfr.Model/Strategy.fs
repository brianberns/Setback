namespace Setback.DeepCfr.Model

open System

open MathNet.Numerics.LinearAlgebra

open PlayingCards
open Setback

module Strategy =

    /// Creates a random strategy of the given length.
    let random n =
        DenseVector.create n (1.0f / float32 n)

    /// Computes strategy from the given per-action regrets.
    /// A strategy is normalized so that its elements sum
    /// to 1.0 (to represent action probabilities).
    let private matchRegrets regrets =

            // find highest-value action
        let idx = Vector.maxIndex regrets

            // scale if possible, or choose highest-value action
        if regrets[idx] > 0.0f then
            let clamped = Vector.map (max 0.0f) regrets   // clamp negative regrets
            clamped / Vector.sum clamped
        else
            DenseVector.init regrets.Count (fun i ->
                if i = idx then 1.0f
                else 0.0f)

    /// Converts a wide vector (indexed by entire deck) to
    /// a narrow vector (indexed by legal actions).
    let private toNarrow (legalActions : _[]) (wide : Vector<_>) =
        assert(wide.Count = Card.numCards)
        legalActions
            |> Seq.map (function
                | MakePlay card ->
                    Card.toIndex card |> Vector.get wide
                | MakeBid _ -> failwith "Bidding not supported")
            |> DenseVector.ofSeq

    /// Converts a narrow vector (indexed by legal actions) to
    /// a wide vector (indexed by entire deck).
    let toWide (legalActions : _[]) (narrow : Vector<float32>) =
        assert(narrow.Count = legalActions.Length)
        let legalPlays =
            legalActions
                |> Seq.map (function
                    | MakePlay card -> card
                    | MakeBid _ -> failwith "Bidding not supported")
        Seq.zip legalPlays narrow
            |> Encoding.encodeCardValues
            |> DenseVector.ofArray

    /// Computes strategies for the given info sets using the
    /// given advantage model.
    let getFromAdvantage model infoSets =

        if Array.length infoSets > 0 then

                // run model on GPU
            use advantages =
                AdvantageModel.getAdvantages infoSets model
            assert(advantages.shape[0] = infoSets.Length)

                // access data on CPU
            let nCols = int advantages.shape[1]
            assert(nCols = Model.outputSize)
            let data =
                use accessor = advantages.data<float32>()
                accessor.ToArray()
            [|
                for iRow, infoSet in Seq.indexed infoSets do
                    let iStart = iRow * nCols
                    data[iStart .. iStart + nCols - 1]
                        |> DenseVector.ofSeq
                        |> toNarrow infoSet.LegalActions
                        |> matchRegrets
            |]

        else Array.empty

    /// Creates a Setback player using the given model.
    let createPlayer model =

        let rng = Random()   // each player has its own RNG

        let act infoSet =
            let strategy =
                getFromAdvantage model [|infoSet|]
                    |> Array.exactlyOne
            Vector.sample rng strategy
                |> Array.get infoSet.LegalActions

        { Act = act }
