namespace Setback.Cfrm

#if !FABLE_COMPILER
open System
open System.Buffers
open FastCfr
#endif

open PlayingCards
open Setback    

module BaselineGameState =

    /// String representation for player-private auction information.
    module AuctionHand =

        /// String representation of a hand.
        let layout =
            SpanLayout.ofLength Setback.numCardsPerHand
                |> Array.replicate BidAction.numSuitsMax
                |> SpanLayout.combine

        /// String representation of a single suit.
        let private copySuitTo ranks (span : Span<_>) =
            assert(span.Length = Setback.numCardsPerHand)
            let minRank = Array.min ranks
            for iRank = 0 to Setback.numCardsPerHand - 1 do
                let cRank =
                    if iRank < ranks.Length then
                        let rank = ranks[iRank]
                        if rank >= Rank.Ten || rank = minRank then
                            rank.Char
                        else 'x'
                    else '.'
                span.Slice(iRank, 1).Fill(cRank)

        /// String representation of a hand.
        let copyTo (span : Span<_>) hand =
            assert(span.Length = layout.Length)
            let ranksArrays =
                hand
                    |> BidAction.chooseTrumpRanks
                    |> Array.map snd
            assert(ranksArrays.Length <= BidAction.numSuitsMax)
            for iRanks = 0 to BidAction.numSuitsMax - 1 do
                let slice = layout.Slice(iRanks, span)
                if iRanks < ranksArrays.Length then
                    copySuitTo ranksArrays[iRanks] slice
                else
                    assert(iRanks > 0)
                    slice.Fill('.')

    /// String representation for player-private playout information.
    module PlayoutHand =

        /// String representation of playout actions.
        let layout =
            Array.replicate PlayAction.maxPerHand DealAction.layout
                |> SpanLayout.combine

        /// String representation of playout actions.
        let copyTo (span : Span<_>) (dealActions : _[]) =
            assert(dealActions.Length <= PlayAction.maxPerHand)
            assert(span.Length = layout.Length)
            for iAction = 0 to dealActions.Length - 1 do
                let action = dealActions[iAction]
                let slice = layout.Slice(iAction, span)
                DealAction.copyTo slice action
            span.Slice(2 * dealActions.Length).Fill('.')

    /// String representation for public+private playout information.
    module AbstractPlayoutPlus =

        /// String representation of public+private playout.
        let layout =
            [|
                AbstractPlayout.layout
                PlayoutHand.layout
            |] |> SpanLayout.combine

        /// String representation of public+private playout.
        let copyTo (span : Span<_>) dealActions handLowTrumpRankOpt playout =
            assert(layout.Length = span.Length)

            let slice = layout.Slice(0, span)
            AbstractPlayout.copyTo
                slice
                handLowTrumpRankOpt
                playout

            let slice = layout.Slice(1, span)
            PlayoutHand.copyTo slice dealActions

    /// String representation for public+private auction information.
    module AbstractAuctionPlus =

        /// String representation of public+private auction.
        let layout =
            let nFill =
                AbstractPlayoutPlus.layout.Length
                    - AbstractAuction.layout.Length
                    - AuctionHand.layout.Length
            [|
                AbstractAuction.layout      // auction
                AuctionHand.layout          // hand
                SpanLayout.ofLength nFill   // fill
            |] |> SpanLayout.combine

        /// String representation of public+private auction.
        let copyTo (span : Span<_>) hand auction =
            assert(layout.Length = span.Length)

                // auction
            let slice = layout.Slice(0, span)
            AbstractAuction.copyTo slice auction

                // hand
            let slice = layout.Slice(1, span)
            AuctionHand.copyTo slice hand

                // fill
            layout.Slice(2, span).Fill('.')

    /// String representation for establishing trump.
    module EstablishTrump =

        /// String representation for establishing trump.
        let layout =
            let nFill =
                AbstractPlayoutPlus.layout.Length
                    - 1
                    - 1
                    - AuctionHand.layout.Length
            [|
                SpanLayout.ofLength 1       // header
                SpanLayout.ofLength 1       // high bid
                AuctionHand.layout          // hand
                SpanLayout.ofLength nFill   // fill
            |] |> SpanLayout.combine

        /// String representation for establishing trump.
        let copyTo (span : Span<_>) hand (auction : AbstractAuction) =
            assert(layout.Length = span.Length)
            assert(auction |> AbstractAuction.isComplete)

                // header
            layout.Slice(0, span).Fill('E')

                // high bid
            let cBid =
                auction.HighBid.Bid
                    |> int
                    |> Char.fromDigit
            layout.Slice(1, span).Fill(cBid)

                // hand
            let slice = layout.Slice(2, span)
            AuctionHand.copyTo slice hand

                // fill
            layout.Slice(3, span).Fill('.')

    /// String representation of an open deal.
    let copyTo (span : Span<_>) dealActions (openDeal : AbstractOpenDeal) =

        let closedDeal = openDeal.ClosedDeal
        let auction = closedDeal.Auction
        let hand = AbstractOpenDeal.currentHand openDeal

        match closedDeal.PlayoutOpt with

                // auction
            | None ->
                AbstractAuctionPlus.copyTo span hand auction

                // playout
            | Some playout ->

                    // establish trump?
                if playout.TrumpOpt.IsNone then
                    EstablishTrump.copyTo span hand auction

                    // play normally
                else
                    let handLowTrumpRankOpt =
                        openDeal
                            |> AbstractOpenDeal.currentLowTrumpRankOpt
                    AbstractPlayoutPlus.copyTo
                        span
                        dealActions
                        handLowTrumpRankOpt
                        playout

    /// Unique identifier for the given deal.
    /// See https://www.stevejgordon.co.uk/creating-strings-with-no-allocation-overhead-using-string-create-csharp.
    let getKey dealActions openDeal =
        let str =
            assert(AbstractAuctionPlus.layout.Length
                = AbstractPlayoutPlus.layout.Length)

#if FABLE_COMPILER
            System.String.Create(
#else
            String.Create(
#endif
                AbstractPlayoutPlus.layout.Length,
                struct (dealActions, openDeal),
                SpanAction(fun span struct (dealActions, openDeal) ->
#if DEBUG
                    span.Fill('?')
#endif
                    copyTo span dealActions openDeal))
        assert(str.Contains('?') |> not)
        str.TrimEnd('.')   // shorten key to save space if possbile

#if !FABLE_COMPILER
    /// Final payoffs for this game.
    let private createTerminalGameState openDeal =

        let score =
            if openDeal.ClosedDeal.PlayoutOpt.IsSome then

                    // compute reward score for this deal
                let score =
                    openDeal
                        |> AbstractOpenDeal.dealScore

                    // transform to zero-sum
                let delta =
                    score
                        |> AbstractScore.delta 0
                        |> float
                [| delta; -delta |]

                // no high bidder
            else
                assert(openDeal.ClosedDeal.Auction.HighBid = AbstractHighBid.none)
                Array.replicate Setback.numTeams 0.0

        score
            |> TerminalGameState.create
            |> Terminal

    /// Current player's team's 0-based index, relative to the dealer's team.
    /// (This is at the team level, rather than the player level, in order to
    /// model Setback as a two-player game.)
    let currentPlayerIdx openDeal =
        let iPlayer =
            openDeal
                |> AbstractOpenDeal.currentPlayerIndex
        iPlayer % Setback.numTeams

    let rec private createNonTerminalGameState openDeal =
        let dealActions = openDeal |> AbstractOpenDeal.getActions
        NonTerminal {
            ActivePlayerIdx = currentPlayerIdx openDeal
            InfoSetKey = getKey dealActions openDeal
            LegalActions = dealActions
            AddAction =
                fun action ->
                    AbstractOpenDeal.addAction action openDeal
                        |> createGameState
        }

    and createGameState openDeal =
        if openDeal |> AbstractOpenDeal.isExhausted then
            createTerminalGameState openDeal
        else
            createNonTerminalGameState openDeal
#endif
