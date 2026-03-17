namespace Setback.Generate

open System

open MathNet.Numerics.LinearAlgebra

open Setback
open Setback.Learn
open Setback.Model

/// Initial node state, awaiting strategy.
type GetStrategy =
    {
        /// Information set in this node.
        InformationSet : InformationSet

        /// Leads to a node in one of the other two states.
        Continuation : Vector<float32> (*per-action strategy*) -> Node
    }

/// Node state awaiting complete children.
and GetUtility =
    {
        /// Information set in this node.
        InformationSet : InformationSet

        /// Incomplete child nodes.
        Children : Node[]

        /// Leads to a completed node.
        Continuation : Complete[] (*children*) -> Node
    }

/// Final node state.
and Complete =
    {
        /// Per-player utility of this node.
        Utilities : float32[]

        /// Sample representing this node.
        SampleOpt : Option<AdvantageSample>

        /// Complete children.
        Children : Complete[]
    }

/// Game state.
and Node =

    /// Awaiting strategy.
    | GetStrategy of GetStrategy

    /// Awaiting utility.
    | GetUtility of GetUtility

    /// Final.
    | Complete of Complete

module Node =

    /// Creates a node.
    let getStrategy infoSet cont =
        GetStrategy {
            InformationSet = infoSet
            Continuation = cont
        }

    /// Creates a node.
    let getUtility infoSet children cont =
        GetUtility {
            InformationSet = infoSet
            Children = children
            Continuation = cont
        }

    /// Creates a node.
    let complete utilities sampleOpt children =
        Complete {
            Utilities = utilities
            SampleOpt = sampleOpt
            Children = children
        }

module Traverse =

    /// Evaluates the utility of the given game.
    let traverse settings iter game =

        /// Top-level loop.
        let rec loop game depth =
            match game.Current with

                    // game is not over
                | Choice1Of2 _ ->
                    loopNonTerminal game depth

                    // game is over
                | Choice2Of2 team ->
                    let payoff =
                        match int team with
                            | 0 -> [|  1f; -1f |]
                            | 1 -> [| -1f;  1f |]
                            | _ -> failwith "Unexpected"
                    Node.complete
                        payoff
                        None
                        Array.empty

        /// Recurses for non-terminal game state.
        and loopNonTerminal game depth =
            let infoSet = Game.currentInfoSet game
            let legalActions = infoSet.LegalActions
            if legalActions.Length = 1 then
                addLoop game depth legalActions[0]                    // forced action
            else
                    // get utility of current player's strategy
                let rnd =
                    lock game.Random (fun () ->                       // lock RNG to allow multi-threaded batch inference
                        game.Random.NextDouble())
                let threshold =
                    settings.SampleBranchRate
                        / (settings.SampleBranchRate + float depth)   // nodes near the root have a greater chance of being expanded
                let getUtility =
                    if rnd <= threshold then getFullUtility
                    else getOneUtility
                let cont = getUtility infoSet game depth
                Node.getStrategy infoSet cont

        /// Adds the given action to the given deal and loops.
        and addLoop game depth action =
            let game = Game.addAction action game
            loop game depth

        /// Gets the full utility of the given info set.
        and getFullUtility infoSet game depth strategy =
            let legalActions = infoSet.LegalActions
            let results =
                legalActions
                    |> Array.map (addLoop game (depth+1))

            let cont children =

                    // get utility of each action
                let actionUtilities =
                    children
                        |> Array.map _.Utilities
                        |> DenseMatrix.ofColumnArrays
                assert(actionUtilities.ColumnCount = legalActions.Length)
                assert(actionUtilities.RowCount = Seat.numSeats)

                    // utility of this info set is action utilities weighted by action probabilities
                let utility = actionUtilities * strategy
                assert(utility.Count = Seat.numSeats)
                let sample =
                    let encoding = Encoding.encode infoSet
                    let wideRegrets =
                        let idx = int infoSet.Player
                        (actionUtilities.Row(idx) - utility[idx])
                            |> Strategy.toWide legalActions
                    AdvantageSample.create encoding wideRegrets iter
                Node.complete
                    (utility.ToArray())
                    (Some sample)
                    children

            Node.getUtility infoSet results cont

        /// Gets the utility of the given info set by
        /// sampling a single action.
        and getOneUtility infoSet game depth strategy =
            let result =
                lock game.Random (fun () ->   // lock RNG to allow multi-threaded batch inference
                    Vector.sample game.Random strategy)
                    |> Array.get infoSet.LegalActions
                    |> addLoop game (depth+1)
            Node.getUtility
                infoSet
                [| result |]
                (Array.exactlyOne >> Complete)

        loop game 0
