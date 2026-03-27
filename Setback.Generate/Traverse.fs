namespace Setback.Generate

open System

open MathNet.Numerics.LinearAlgebra

open PlayingCards
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

    /// Gets payoffs for the given deal score.
    let private getPayoffs (dealScore : Score) =
        assert(Team.numTeams = 2)
        let nsScore =
            float32 (
                dealScore[Team.NorthSouth]
                    - dealScore[Team.EastWest])
        let payoffs =
            [|
                for team in Enum.getValues<Team> do
                    match team with
                        | Team.NorthSouth -> nsScore
                        | Team.EastWest -> -nsScore
                        | _ -> failwith "Unexpected"
            |]
        Node.complete payoffs None Array.empty

    /// Answers the current player's information set.
    let private currentInfoSet deal =
        let player = OpenDeal.currentPlayer deal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create
            player hand deal.ClosedDeal Score.zero   // assume 0-0 score

    /// Evaluates the utility of the given deal.
    let traverse settings iter deal =

        /// Top-level loop.
        let rec loop (deal : OpenDeal) depth =
            if OpenDeal.isComplete deal then
                deal.ClosedDeal
                    |> ClosedDeal.getDealScore 
                    |> getPayoffs
            else loopNonTerminal deal depth   // continue current deal

        /// Recurses for non-terminal deal state.
        and loopNonTerminal deal depth =
            let infoSet = currentInfoSet deal
            let legalActions = infoSet.LegalActions
            if legalActions.Length = 1 then
                addLoop deal depth legalActions[0]                    // forced action
            else
                    // get utility of current player's strategy
                let rnd = Random.Shared.NextDouble()                  // thread-safety needed
                let threshold =
                    settings.SampleBranchRate
                        / (settings.SampleBranchRate + float depth)   // nodes near the root have a greater chance of being expanded
                let getUtility =
                    if rnd <= threshold then getFullUtility
                    else getOneUtility
                let cont = getUtility infoSet deal depth
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
                    |> Array.map (addLoop deal (depth+1))

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
                    let encoding = Encoding.encode infoSet
                    let wideRegrets =
                        let idx = int (Team.ofSeat infoSet.Player)
                        (actionUtilities.Row(idx) - utility[idx])
                            |> Strategy.toWide legalActions
                    AdvantageSample.create encoding wideRegrets iter
                Node.complete
                    (utility.ToArray())
                    (Some sample)
                    children

            Node.getUtility infoSet results cont

        /// Gets the utility of the given info set by sampling
        /// a single action.
        and getOneUtility infoSet deal depth strategy =
            let result =
                Vector.sample Random.Shared strategy   // thread-safety needed
                    |> Array.get infoSet.LegalActions
                    |> addLoop deal (depth+1)
            Node.getUtility
                infoSet
                [| result |]
                (Array.exactlyOne >> Complete)

        loop deal 0
