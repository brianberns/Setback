namespace Setback.Cfrm

open System

#if !FABLE_COMPILER
open System.Buffers
open Cfrm
#endif

open PlayingCards
open Setback

module BootstrapGameState =

    /// Which team (if any) won the game with the given score.
    let winningTeamOpt (AbstractScore scores) =
        let maxScore = Array.max scores
        if maxScore >= Setback.winThreshold then
            scores
                |> Seq.indexed
                |> Seq.where (fun (_, score) ->
                    score = maxScore)
                |> Seq.map fst
                |> Seq.tryExactlyOne
        else None

    module AbstractScore =

        /// Converts the given score to deal points needed to win
        /// the game.
        let toNeed (AbstractScore scores) =
            let winThreshold =
                seq {
                    for score in scores do
                        yield score + 1
                    yield Setback.winThreshold
                } |> Seq.max
            assert(Setback.numTeams = 2)
            winThreshold - scores[0],  // "us" need
            winThreshold - scores[1]   // "them" need

        /// String representation of the given game score, which is
        /// relative to the current player's team.
        let toAbbr (gameScore : AbstractScore) =

            let toChar need =
                if need <= Setback.numDealPoints then
                    "0123456789"[need]
                else 'x'

            let usNeed, themNeed = toNeed gameScore
            let cThemNeed = toChar themNeed
            sprintf "%c%c"
                cThemNeed
                (if cThemNeed <> 'x' && usNeed > themNeed then '!'
                else '.')

    module AbstractHighBid =

        /// String representation of a high bid.
        let toAbbr (absHighBid : AbstractHighBid) =
            sprintf "%d/%d"
                (absHighBid.Bid |> int)
                absHighBid.BidderIndex

    module AbstractAuction =

        /// String representation of an auction.
        let toAbbr (absAuction : AbstractAuction) =
            sprintf "%s/%d"
                (absAuction.HighBid |> AbstractHighBid.toAbbr)
                absAuction.NumBids

    module Hand =

        open BaselineGameState

        /// String representation of a hand.
        let toAbbr (hand : Hand) =
            String.Create(
                AuctionHand.layout.Length,
                hand,
                SpanAction(AuctionHand.copyTo))

    /// String representation of the given game state.
    let toAbbr auction (gameScore : AbstractScore) hand =

            // shift from dealer-relative score to "us" vs. "them" score
        let gameScore =
            let iCurTeam =   // index of current team relative to dealer
                (auction
                    |> AbstractAuction.currentBidderIndex)
                    % Setback.numTeams
            gameScore |> AbstractScore.shift iCurTeam

        sprintf "%s/%s/%s"
            (AbstractScore.toAbbr gameScore)
            (AbstractAuction.toAbbr auction)
            (Hand.toAbbr hand)

#if !FABLE_COMPILER
    /// Plays a card in the given deal using the given baseline strategy
    /// profile.
    let play (baselineProfile : StrategyProfile) deal =

            // get legal plays in this situation
        let hand =
            AbstractOpenDeal.currentHand deal
        let handLowTrumpRankOpt =
            AbstractOpenDeal.currentLowTrumpRankOpt deal
        let playout, legalPlays =
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout ->
                    let legalPlays =
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.toArray
                    playout, legalPlays
                | _ -> failwith "Unexpected"
        match legalPlays.Length with
            | 0 -> failwith "Unexpected"
            | 1 -> legalPlays[0]   // trivial case

                // must choose between multiple legal plays
            | _ ->

                    // get legal actions (not usually 1:1 with legal plays)
                let legalPlayActions =
                    playout
                        |> PlayAction.getActions hand handLowTrumpRankOpt

                let action =
                    match legalPlayActions.Length with
                        | 0 -> failwith "Unexpected"
                        | 1 -> legalPlayActions[0]   // trivial case

                            // choose action
                        | _ ->
                                // determine key for this situation
                            let key =
                                let legalDealActions =
                                    legalPlayActions
                                        |> Array.map DealPlayAction
                                deal
                                    |> BaselineGameState.getKey legalDealActions

                                // profile contains key?
                            baselineProfile.Best(key)
                                |> Option.map (fun iAction ->
                                    legalPlayActions[iAction])

                                    // fallback
                                |> Option.defaultWith (fun () ->
                                    legalPlayActions[0])

                    // convert action to card
                PlayAction.getPlay
                    hand
                    handLowTrumpRankOpt
                    playout
                    action

/// State of a Setback game for counterfactual regret minimization
/// of score-sensitive bidding behavior.
type BootstrapGameState
    (baselineProfile : StrategyProfile,
    openDeal : AbstractOpenDeal,
    gameScore : AbstractScore) =
    inherit BaselineGameState(openDeal)

    /// Final payoffs for this game, if it is now over.
    override __.TerminalValuesOpt =
        if openDeal |> AbstractOpenDeal.isExhausted then
            if openDeal.ClosedDeal.PlayoutOpt.IsSome then

                    // dealer's team's delta
                let delta =

                        // final score of this deal
                    let dealScore =
                        openDeal
                            |> AbstractOpenDeal.dealScore

                        // update game score as a result of this deal
                    let gameScore =
                        gameScore + dealScore

                        // compute reward
                    match BootstrapGameState.winningTeamOpt gameScore with

                            // reward for winning the game (regardless of deal score)
                        | Some iWinningTeam ->
                            let reward = 5.5   // value determined empirically
                            if iWinningTeam = 0 then reward
                            else -reward

                            // deal score determines delta directly
                        | None ->
                            dealScore
                                |> AbstractScore.delta 0
                                |> float

                    // zero-sum
                Some [| delta; -delta |]

                // no high bidder
            else
                assert(openDeal.ClosedDeal.Auction.HighBid = AbstractHighBid.none)
                Array.replicate Setback.numTeams 0.0 |> Some
        else None

    /// This state's unique identifier.
    override __.Key =
        let hand =
            let iPlayer =
                openDeal
                    |> AbstractOpenDeal.currentPlayerIndex
            openDeal.UnplayedCards[iPlayer]
        assert(hand.Count = Setback.numCardsPerHand)
        BootstrapGameState.toAbbr
            openDeal.ClosedDeal.Auction
            gameScore
            hand

    /// Actions available to the current player in this state.
    override __.LegalActions =
        openDeal.ClosedDeal.Auction
            |> AbstractAuction.legalBids
            |> Array.map (BidAction >> DealBidAction)

    /// Takes the given action, which moves the game to a new state.
    override __.AddAction(action) =
        assert(
            match action with
                | DealBidAction _ -> true
                | DealPlayAction _ -> false)

        let openDeal =
            openDeal
                |> AbstractOpenDeal.addAction action

        let openDeal, gameScore =
            if openDeal.ClosedDeal.Auction |> AbstractAuction.isComplete then
                match openDeal.ClosedDeal.PlayoutOpt with

                        // complete playout
                    | Some playout ->
                        assert(playout.History.NumTricksCompleted = 0)
                        assert(playout.CurrentTrick.NumPlays = 0)

                        let openDeal =
                            (openDeal, [1 .. Setback.numCardsPerDeal])
                                ||> Seq.fold (fun openDeal _ ->
                                    let card =
                                        BootstrapGameState.play
                                            baselineProfile
                                            openDeal
                                    openDeal |> AbstractOpenDeal.addPlay card)
                        assert(openDeal |> AbstractOpenDeal.isComplete)

                            // compute resulting game score (to-do: avoid doing this twice)
                        let dealScore =
                            openDeal |> AbstractOpenDeal.dealScore
                        let gameScore = gameScore + dealScore
                        openDeal, gameScore

                        // no bidder
                    | None -> openDeal, gameScore

                // continue auction
            else openDeal, gameScore

        BootstrapGameState(baselineProfile, openDeal, gameScore) :> _
#endif
