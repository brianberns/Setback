namespace Setback.DeepCfr.Learn

open System

open MathNet.Numerics.LinearAlgebra

open Setback
open Setback.DeepCfr.Model

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
        /// Per-team utility of this node.
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

/// Model Setback as a zero-sum game.
module private ZeroSum =

    /// Gets the payoff for the given deal score from each
    /// team's point of view.
    let getPayoff score =
        let sum =
            let points = score.ScoreMap.Values
            assert(points.Count = Team.numTeams)
            Seq.sum points
        score.ScoreMap
            |> Map.map (fun _ pt ->
                let otherAvg =
                    float32 (sum - pt)
                        / float32 (Team.numTeams - 1)
                otherAvg - float32 pt)

    /// Computes the payoff for the given deal, if it is
    /// complete.
    let tryGetPayoff deal =
        OpenDeal.tryGetDealScore deal
            |> Option.map getPayoff

module Traverse =

    /// Evaluates the utility of the given deal.
    let traverse iter deal (rng : Random) =

        /// Top-level loop.
        let rec loop deal depth =
            match ZeroSum.tryGetPayoff deal with
                | Some payoff ->
                    Node.complete   // deal is over
                        (Seq.toArray payoff.Values)
                        None
                        Array.empty
                | None ->
                    loopNonTerminal deal depth

        /// Recurses for non-terminal game state.
        and loopNonTerminal deal depth =
            let infoSet = OpenDeal.currentInfoSet deal
            let legalActions = infoSet.LegalActions
            if legalActions.Length = 1 then
                addLoop deal depth legalActions[0]   // forced action
            else
                    // get utility of current player's strategy
                let rnd = lock rng (fun () -> rng.NextDouble())
                let threshold =
                    settings.SampleDecay
                        / (settings.SampleDecay + float depth)
                let getUtility =
                    if rnd <= threshold then getFullUtility
                    else getOneUtility
                let cont =
                    getUtility infoSet deal depth
                Node.getStrategy infoSet cont

        /// Adds the given action to the given deal and loops.
        and addLoop deal depth action =
            let deal = OpenDeal.addAction action deal
            loop deal depth

        /// Gets the full utility of the given info set.
        and getFullUtility infoSet deal depth strategy =
            let legalActions = infoSet.LegalActions
            let results =
                legalActions
                    |> Array.map (
                        addLoop deal (depth+1))

            let cont children =

                    // get utility of each action
                let actionUtilities =
                    children
                        |> Array.map _.Utilities
                        |> DenseMatrix.ofColumnArrays
                assert(actionUtilities.ColumnCount = legalActions.Length)
                assert(actionUtilities.RowCount = Team.numTeams)

                    // utility of this info set is action utilities weighted by action probabilities
                let utility = actionUtilities * strategy
                assert(utility.Count = Team.numTeams)
                let sample =
                    let wideRegrets =
                        let idx = int infoSet.Player
                        (actionUtilities.Row(idx) - utility[idx])
                            |> Strategy.toWide legalActions
                    AdvantageSample.create infoSet wideRegrets iter
                Node.complete
                    (utility.ToArray())
                    (Some sample)
                    children

            Node.getUtility infoSet results cont

        /// Gets the utility of the given info set by
        /// sampling a single action.
        and getOneUtility infoSet deal depth strategy =
            let result =
                lock rng (fun () ->
                    Vector.sample rng strategy)
                    |> Array.get infoSet.LegalActions
                    |> addLoop deal (depth+1)
            Node.getUtility
                infoSet
                [|result|]
                (Array.exactlyOne >> Complete)

        loop deal 0
