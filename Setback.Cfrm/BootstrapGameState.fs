namespace Setback.Cfrm

open System
open System.Buffers

open Cfrm

open PlayingCards
open Setback

module BootstrapGameState =

    /// Which team (if any) won the game with the given score.
    let winningTeamScoreOpt (AbstractScore scores) =
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

        /// Converts the given score to deal points needed to win the
        /// game, relative to the dealer's team.
        let toNeed (AbstractScore scores) =
            let winThreshold =
                seq {
                    for score in scores do
                        yield score + 1
                    yield Setback.winThreshold
                } |> Seq.max
            assert(Setback.numTeams = 2)
            winThreshold - scores.[0], winThreshold - scores.[1]

        /// String representation of a game score.
        let toAbbr (gameScore : AbstractScore) =

            let toChar need =
                if need <= Setback.numDealPoints then
                    "0123456789".[need]
                else 'x'

            let usNeed, themNeed = toNeed gameScore
            let cThemNeed = toChar themNeed
            sprintf "%c%c"
                cThemNeed
                (if cThemNeed <> 'x' && usNeed > themNeed then '!' else '.')

    module AbstractAuction =

        /// String representation of an auction.
        let toAbbr (auction : AbstractAuction) =
            sprintf "%d"
                (auction.HighBid.Bid |> int)

    module Hand =

        open BaselineGameState

        /// String representation of a hand.
        let toAbbr (hand : Hand) =
            String.Create(
                AuctionHand.layout.Length,
                hand,
                SpanAction(AuctionHand.copyTo))

    // String representation of the given game state.
    let toAbbr auction (gameScore : AbstractScore) hand =
        let gameScore =
            let iCurTeam =   // index of current team relative to dealer
                (auction
                    |> AbstractAuction.currentBidderIndex)
                    % Setback.numTeams
            AbstractScore [|
                for iTeam = 0 to Setback.numTeams - 1 do
                    yield gameScore.[(iTeam + iCurTeam) % Setback.numTeams]   // relative to current team
            |]
        sprintf "%s/%s/%s"
            (AbstractScore.toAbbr gameScore)
            (AbstractAuction.toAbbr auction)
            (Hand.toAbbr hand)

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
            | 1 -> legalPlays.[0]   // trivial case

                // must choose between multiple legal plays
            | _ ->

                    // get legal actions (not usually 1:1 with legal plays)
                let legalPlayActions =
                    playout
                        |> PlayAction.getActions hand handLowTrumpRankOpt

                let action =
                    match legalPlayActions.Length with
                        | 0 -> failwith "Unexpected"
                        | 1 -> legalPlayActions.[0]   // trivial case

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
                                    legalPlayActions.[iAction])

                                    // fallback
                                |> Option.defaultWith (fun () ->
                                    legalPlayActions.[0])

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
            match openDeal.ClosedDeal.PlayoutOpt with
                | Some playout ->

                        // dealer's team's delta
                    let delta =

                            // final score of this deal
                        let dealScore =

                                // compute raw deal score (before Setback penalty)
                            let dealScoreRaw =
                                openDeal
                                    |> AbstractOpenDeal.dealScore

                                // apply Setback penalty, if necessary
                            playout
                                |> AbstractPlayout.finalizeDealScore dealScoreRaw

                            // update game score as a result of this deal
                        let gameScore =
                            gameScore + dealScore

                            // compute reward
                        match BootstrapGameState.winningTeamScoreOpt gameScore with

                                // reward for winning the game (regardless of deal score)
                            | Some iWinningTeam ->
                                let reward = 5.0   // value determined empirically
                                if iWinningTeam = 0 then reward
                                else -reward

                                // deal score determines delta directly
                            | None ->
                                dealScore
                                    |> AbstractScore.delta 0
                                    |> float

                        // zero-sum
                    [| delta; -delta |]

                    // no high bidder
                | None ->
                    assert(openDeal.ClosedDeal.Auction.HighBid = AbstractHighBid.none)
                    Array.replicate Setback.numTeams 0.0

                |> Some
        else None

    /// This state's unique identifier.
    override __.Key =
        let hand =
            let iPlayer =
                openDeal
                    |> AbstractOpenDeal.currentPlayerIndex
            openDeal.UnplayedCards.[iPlayer]
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

                            // compute resulting game score
                        let dealScore =
                            let dealScoreRaw =
                                openDeal |> AbstractOpenDeal.dealScore
                            playout
                                |> AbstractPlayout.finalizeDealScore dealScoreRaw
                        let gameScore = gameScore + dealScore
                        openDeal, gameScore

                        // no bidder
                    | None -> openDeal, gameScore

                // continue auction
            else openDeal, gameScore

        BootstrapGameState(baselineProfile, openDeal, gameScore) :> _
