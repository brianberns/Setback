namespace Setback.Cfrm

open System

open PlayingCards
open Setback

/// An action that applies when leading a card on a trick.
type LeadAction =

    /// Establish trump on the first play of the first trick.
    | EstablishTrump of int (*normalized suit index*)

    /// Lead a trump card on first play of a subsequent trick.
    | LeadTrump of Option<Rank>

    /// Lead a strong non-trump card.
    | LeadStrong

    /// Lead a weak non-trump card.
    | LeadWeak

module LeadAction =

    /// Creates a lead action for the given card in the given
    /// playout.
    let createOpt
        hand
        lowTrumpRankOpt
        (playout : AbstractPlayout)
        (card : Card) =
        assert(playout.CurrentTrick.NumPlays = 0)

        match playout.TrumpOpt with
            | None ->
                let suit0, suit1Opt =
                    let suits =
                        BidAction.chooseTrumpRanks hand
                            |> Array.map fst
                    let suit1Opt =
                        if suits.Length > 1 then
                            Some suits.[1]
                        else None
                    suits.[0], suit1Opt
                if card.Suit = suit0 then
                    EstablishTrump 0 |> Some
                elif Some card.Suit = suit1Opt then
                    EstablishTrump 1 |> Some
                else None
            | Some trump ->
                if card.Suit = trump then
                    assert(Some card.Rank >= lowTrumpRankOpt)
                    if card.Rank.GamePoints > 0
                        || Some card.Rank = lowTrumpRankOpt then
                        LeadTrump (Some card.Rank)
                    else
                        LeadTrump None
                elif card.Rank > Rank.Ten then
                    LeadStrong
                else
                    LeadWeak
                |> Some

    /// Actions available in the given situation, sorted for
    /// reproducibility.
    let getActions hand handLowTrumpRankOpt playout =
        let lowTrumpRankOpt =
            assert(
                Rank.lower
                    (playout.History.LowTakenOpt |> Option.map fst)
                    playout.CurrentTrick.LowTrumpRankOpt
                    = playout.CurrentTrick.LowTrumpRankOpt)
            Rank.lower
                handLowTrumpRankOpt
                playout.CurrentTrick.LowTrumpRankOpt
        let actions =
            playout
                |> AbstractPlayout.legalPlays hand
                |> Seq.choose (
                    createOpt
                        hand
                        lowTrumpRankOpt
                        playout)
                |> Seq.distinct
                |> Seq.sort
                |> Seq.toArray
        assert(actions.Length > 0)
        actions

    /// Makes the first lead of a deal.
    let establishTrumpRank (ranks : _[]) =
        assert(ranks.Length > 0)

            // find highest rank that could typically be low
        let highestLowRank =
            assert(
                ranks
                    |> Seq.pairwise
                    |> Seq.forall (fun (rank1, rank2) ->
                        rank1 > rank2))
            ranks
                |> Array.last
                |> min Rank.Four

        ranks
            |> Seq.maxBy (fun rank ->
                let group =
                    if rank > Rank.Jack then 3                             // prefer AKQ
                    elif rank < Rank.Ten && rank > highestLowRank then 2   // then 98...
                    elif rank = Rank.Ten then 1                            // then T
                    elif rank = Rank.Jack then 0                           // then J
                    else -1                                                // then ...32
                group, rank)

    /// Finds a card to play corresponding to the given action in
    /// the given situation.
    let getPlay hand handLowTrumpRankOpt (playout : AbstractPlayout) action =
        assert(playout.CurrentTrick.NumPlays = 0)
        let lowTrumpRankOpt =
            assert(
                Rank.lower
                    (playout.History.LowTakenOpt |> Option.map fst)
                    playout.CurrentTrick.LowTrumpRankOpt
                    = playout.CurrentTrick.LowTrumpRankOpt)
            Rank.lower
                handLowTrumpRankOpt
                playout.CurrentTrick.LowTrumpRankOpt
        match playout.TrumpOpt with
            | None ->
                let suitRanks =
                    BidAction.chooseTrumpRanks hand
                match action with
                    | EstablishTrump iSuit ->
                        let suit, ranks = suitRanks.[iSuit]
                        let rank = establishTrumpRank ranks
                        let card = Card(rank, suit)
                        assert
                            (playout
                                |> AbstractPlayout.legalPlays hand
                                |> Seq.contains card)
                        card
                    | _ -> failwith "Unexpected"
            | Some trump ->
                match action with
                    | LeadTrump (Some rank) ->
                        let card = Card(rank, trump)
                        assert
                            (playout
                                |> AbstractPlayout.legalPlays hand
                                |> Seq.contains card)
                        card
                    | LeadTrump None ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                assert(card.Suit <> trump
                                    || Some card.Rank >= lowTrumpRankOpt)
                                card.Suit = trump
                                    && card.Rank.GamePoints = 0
                                    && Some card.Rank > lowTrumpRankOpt)
                            |> Seq.maxBy (fun card -> card.Rank)
                    | LeadStrong ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Rank > Rank.Ten)
                            |> Seq.maxBy (fun card -> card.Rank)
                    | LeadWeak ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Rank <= Rank.Ten)
                            |> Seq.minBy (fun card -> card.Rank)
                    | _ -> failwith "Unexpected"

    /// String representation of a follow action.
    let layout =
        [|
            SpanLayout.ofLength 1
            SpanLayout.ofLength 1
        |] |> SpanLayout.combine

    /// String representation of a lead action.
    let copyTo (span : Span<_>) action =
        assert(span.Length = layout.Length)
        let slice0 = layout.Slice(0, span)
        let slice1 = layout.Slice(1, span)
        match action with
            | EstablishTrump iSuit ->
                slice0.Fill('E')
                slice1.Fill(Char.fromDigit iSuit)
            | LeadTrump rankOpt ->
                slice0.Fill('T')
                let cRank =
                    rankOpt
                        |> Option.map (fun rank -> rank.Char)
                        |> Option.defaultValue 'x'
                slice1.Fill(cRank)
            | LeadStrong ->
                slice0.Fill('S')
                slice1.Fill('.')
            | LeadWeak ->
                slice0.Fill('W')
                slice1.Fill('.')
