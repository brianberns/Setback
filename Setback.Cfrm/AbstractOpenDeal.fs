namespace Setback.Cfrm

#if !FABLE_COMPILER
open System.Collections.Immutable
#endif

open PlayingCards
open Setback

/// Abstract view of an open deal, including all players' unplayed
/// cards.
type AbstractOpenDeal =
    {
        /// Underlying closed deal.
        ClosedDeal : AbstractClosedDeal

        /// Each player's unplayed cards, indexed relative to the
        /// dealer.
        UnplayedCards : ImmutableArray<Set<Card>>

        /// Lowest trump card dealt to each player (if any), indexed
        /// relative to the dealer.
        HandLowTrumpRankOpts : ImmutableArray<Option<Rank>>

        /// Rank of highest trump dealt (if trump has been established),
        /// with index of team that holds it, relative to the dealer.
        HighTrumpOpt : Option<Rank * int>

        /// Rank of lowest trump dealt (if trump has been established).
        LowTrumpOpt : Option<Rank>

        /// Jack of trump was dealt (if trump has been established)?
        JackTrumpOpt : Option<bool>

        /// Total game points dealt.
        TotalGamePoints : int
    }

module AbstractOpenDeal =

    /// Creates an open deal from the given hands.
    let fromHands dealer (hands : Map<_, Hand>) =
        {
            ClosedDeal =
                AbstractClosedDeal.initial
            UnplayedCards =
                dealer
                    |> Seat.cycle
                    |> Seq.map (fun seat ->
                        set hands[seat])
                    |> ImmutableArray.CreateRange
            HandLowTrumpRankOpts =
                Seq.replicate Seat.numSeats None
                    |> ImmutableArray.CreateRange
            HighTrumpOpt = None
            LowTrumpOpt = None
            JackTrumpOpt = None
            TotalGamePoints =
                hands
                    |> Map.toSeq
                    |> Seq.collect snd
                    |> Seq.sumBy (fun card -> card.Rank.GamePoints)
        }

    /// Deals cards from the given deck to each player.
    let fromDeck dealer deck =

        let numCardsPerGroup = 3
        assert (Setback.numCardsPerHand % numCardsPerGroup = 0)

        deck.Cards

                // number each card
            |> Seq.mapi (fun iCard card -> iCard, card)

                // assign each card to a player
            |> Seq.groupBy (fun (iCard, _) ->
                dealer |> Seat.incr ((iCard / numCardsPerGroup) + 1))   // deal first group of cards to dealer's left

                // gather each player's cards
            |> Seq.map (fun (seat, pairs) ->
                let hand : Hand =
                    pairs
                        |> Seq.map snd
                        |> Seq.take Setback.numCardsPerHand
                seat, hand)
            |> Map

                // create a deal from these hands
            |> fromHands dealer

    /// Indicates whether the given deal is complete.
    let isComplete deal =
        deal.ClosedDeal
            |> AbstractClosedDeal.isComplete

    /// Indicates whether the final score of the given deal has been
    /// determined, even if the deal is not yet complete. (Note that
    /// the High point can be assigned as soon as trump is established.)
    let isExhausted deal =
        let result =
            match deal.ClosedDeal.PlayoutOpt with
                | None -> isComplete deal
                | Some playout ->

                        // trump is established? (which implies that High is determined)
                    if playout.TrumpOpt.IsSome then
                        let history = playout.History

                            // Low is determined?
                        let lowTakenOpt =
                            history.LowTakenOpt
                                |> Option.map fst
                        if lowTakenOpt = deal.LowTrumpOpt then

                                // Jack is determined?
                            let jackTakenOpt =
                                Some history.JackTakenOpt.IsSome
                            if jackTakenOpt = deal.JackTrumpOpt then

                                    // Game is determined?
                                let gameUntaken =
                                    let gameTaken =
                                        let (AbstractScore points) =
                                            history.GameScore
                                        points |> Array.sum
                                    deal.TotalGamePoints - gameTaken
                                assert(gameUntaken >= 0)
                                if gameUntaken = 0 then
                                    true
                                else
                                    let delta =
                                        history.GameScore
                                            |> AbstractScore.delta 0
                                            |> abs
                                    assert(delta >= 0)
                                    gameUntaken < delta

                            else false
                        else false
                    else false
        assert(result || deal |> isComplete |> not)
        result

    /// Score of this deal (relative to the dealer's team), not
    /// counting any setback penalty.
    let private dealScoreRaw deal =

            // assigns a single point to the given team, if any
        let toScore teamOpt =
            assert(
                match teamOpt with
                    | Some team ->
                        team >= 0 && team < Setback.numTeams
                    | None -> true)
            AbstractScore [|
                for iTeam = 0 to Setback.numTeams - 1 do
                    if teamOpt = Some iTeam then
                        yield 1
                    else
                        yield 0
            |]

            // gather deal points
        match deal.ClosedDeal.PlayoutOpt with
            | Some playout ->

                    // which team is winning the Game point?
                let history = playout.History
                let gameTakenOpt =
                    let deltaSign =
                        history.GameScore
                            |> AbstractScore.delta 0
                            |> sign
                    match deltaSign with
                        |  1 -> Some 0   // dealer's team wins Game point
                        | -1 -> Some 1   // other team wins Game point
                        |  0 -> None     // tie, no Game point
                        |  _ -> failwith "Unexpected"

                let scores =
                    [|
                        deal.HighTrumpOpt |> Option.map snd
                        history.LowTakenOpt |> Option.map snd
                        history.JackTakenOpt
                        gameTakenOpt
                    |] |> Seq.map toScore

                    // accumulate total
                (AbstractScore.zero, scores)
                    ||> Seq.fold (+)

            | None -> AbstractScore.zero

    /// Score of this deal (relative to the dealer's team), including
    /// any setback penalty.
    let dealScore deal =
        let rawScore =
            dealScoreRaw deal
        deal.ClosedDeal.Auction.HighBid
            |> AbstractHighBid.finalizeDealScore rawScore

    /// Index of the current player, relative to the dealer.
    let currentPlayerIndex deal =
        deal.ClosedDeal
            |> AbstractClosedDeal.currentPlayerIndex

    /// Current player's hand.
    let currentHand deal =
        let iPlayer = currentPlayerIndex deal
        deal.UnplayedCards[iPlayer] :> Hand

    /// Current player's lowest dealt trump rank, if any.
    let currentLowTrumpRankOpt deal =
        let iPlayer = currentPlayerIndex deal
        deal.HandLowTrumpRankOpts[iPlayer]

    /// Actions available to the current player with the given
    /// hand in the given deal.
    let getActions deal =
        if isComplete deal then
            Array.empty
        else
            let hand = currentHand deal
            let handLowTrumpRankOpt =
                currentLowTrumpRankOpt deal
            deal.ClosedDeal
                |> AbstractClosedDeal.getActions
                    hand
                    handLowTrumpRankOpt

    /// Adds a bid to the auction of the given deal.
    let addBid bid (deal : AbstractOpenDeal) =
        {
            deal with
                ClosedDeal =
                    deal.ClosedDeal
                        |> AbstractClosedDeal.addBid bid
        }

    /// Plays a card in the given deal.
    let addPlay (card : Card) deal =

            // determine High, Low, and Jack cards when trump is first established
        let highTrumpOpt, lowTrumpOpt, jackTrumpOpt, handLowTrumpRankOpts =

                // already established?
            let playout = deal.ClosedDeal.Playout
            if playout.TrumpOpt.IsSome then
                assert(deal.HighTrumpOpt.IsSome)
                assert(deal.LowTrumpOpt.IsSome)
                assert(deal.JackTrumpOpt.IsSome)
                deal.HighTrumpOpt,
                deal.LowTrumpOpt,
                deal.JackTrumpOpt,
                deal.HandLowTrumpRankOpts

            else
                assert(playout.History.NumTricksCompleted = 0)
                assert(playout.CurrentTrick.NumPlays = 0)
                assert(deal.HighTrumpOpt.IsNone)
                assert(deal.LowTrumpOpt.IsNone)
                assert(deal.JackTrumpOpt.IsNone)
                assert(
                    deal.UnplayedCards
                        |> Seq.concat
                        |> Seq.length = Setback.numCardsPerDeal)
                let trump = card.Suit

                    // determine High, Low, and Jack cards
                let rankTeams =
                    deal.UnplayedCards
                        |> Seq.indexed
                        |> Seq.collect (fun (iPlayer, cards) ->
                            let iTeam = iPlayer % Setback.numTeams
                            cards
                                |> Seq.where (fun c -> c.Suit = trump)
                                |> Seq.map (fun card -> card.Rank, iTeam))
                        |> Seq.toArray
                let ranks = rankTeams |> Seq.map fst
                let highTrumpOpt =
                    rankTeams |> Array.max |> Some
                let lowTrumpOpt =
                    ranks |> Seq.min |> Some
                let jackTrumpOpt =
                    ranks |> Seq.contains Rank.Jack |> Some

                    // determine low trump ranks dealt
                let handLowTrumpRankOpts =
                    deal.UnplayedCards
                        |> Seq.map (fun hand ->
                            hand
                                |> Seq.where (fun card ->
                                    card.Suit = trump)
                                |> Seq.map (fun card ->
                                    card.Rank)
                                |> Seq.tryMin)
                        |> ImmutableArray.CreateRange

                highTrumpOpt,
                lowTrumpOpt,
                jackTrumpOpt,
                handLowTrumpRankOpts

        {
            deal with

                    // add play to underlying deal
                ClosedDeal =
                    deal.ClosedDeal
                        |> AbstractClosedDeal.addPlay card

                    // remove card from player's hand
                UnplayedCards =
                    let iPlayer = currentPlayerIndex deal
                    let hand = deal.UnplayedCards[iPlayer]
                    assert(hand |> Seq.contains card)
                    let hand' = hand.Remove(card)
                    deal.UnplayedCards.SetItem(iPlayer, hand')

                    // update trump points
                HighTrumpOpt = highTrumpOpt
                LowTrumpOpt = lowTrumpOpt
                JackTrumpOpt = jackTrumpOpt

                    // update low trump ranks
                HandLowTrumpRankOpts = handLowTrumpRankOpts
        }

    /// Takes the given action.
    let addAction action deal =
        match action with

                // auction
            | DealBidAction bidAction ->
                let bid =
                    bidAction
                        |> BidAction.getBid
                            deal.ClosedDeal.Auction
                deal |> addBid bid

                // playout
            | DealPlayAction playAction ->
                let card =
                    let hand = currentHand deal
                    let handLowTrumpRankOpt =
                        currentLowTrumpRankOpt deal
                    playAction
                        |> PlayAction.getPlay
                            hand
                            handLowTrumpRankOpt
                            deal.ClosedDeal.Playout
                deal |> addPlay card
