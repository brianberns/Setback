namespace Setback.Cfrm

open System

open PlayingCards
open Setback    

/// An action that applies when playing a card on a trick that
/// has already started.
type FollowAction =

    /// Play specific trump.
    | PlayTrump of Rank

    /// Play unspecified trump to win trick.
    | PlayTrumpWin

    /// Play unspecified trump to lose trick.
    | PlayTrumpLose

    /// Follow suit led to win trick (if not trump).
    | FollowSuitWin of int (*Game points*)

    /// Follow suit led to lose trick (if not trump).
    | FollowSuitLose of int (*Game points*)

    /// Contribute ten (if not trump or suit led).
    | ContributeTen

    /// Contribute other game (if not trump, suit led, or a ten).
    | ContributeGame

    /// Play weakest card (if not trump, suit led, or game).
    | Duck

module FollowAction =

    /// Creates a follow action for the given card in the given playout.
    let create lowTrumpRankOpt (playout : AbstractPlayout) (card : Card) =
        let trick = playout.CurrentTrick
        assert(trick.NumPlays > 0)
        match playout.TrumpOpt, trick.SuitLedOpt, trick.HighPlayOpt with
            | Some trump, Some suitLed, Some highPlay ->
                let highCard = highPlay.Play
                if card.Suit = trump then
                    assert(Some card.Rank >= lowTrumpRankOpt)
                    if card.Rank.GamePoints > 0
                        || Some card.Rank = lowTrumpRankOpt then
                        PlayTrump card.Rank
                    elif highCard.Suit <> trump
                        || card.Rank > highCard.Rank then
                        PlayTrumpWin
                    else
                        PlayTrumpLose
                elif card.Suit = suitLed then
                    if highCard.Suit = suitLed
                        && card.Rank > highCard.Rank then
                        FollowSuitWin card.Rank.GamePoints
                    else
                        FollowSuitLose card.Rank.GamePoints
                else
                    match card.Rank.GamePoints with
                        | 10 -> ContributeTen
                        |  0 -> Duck
                        |  _ -> ContributeGame
            | _ -> failwith "Unexpected"

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
                |> Seq.map (
                    create
                        lowTrumpRankOpt
                        playout)
                |> Seq.distinct
                |> Seq.sort
                |> Seq.toArray
        assert(actions.Length > 0)
        actions

    /// Finds a card to play corresponding to the given action in
    /// the given situation.
    let getPlay hand handLowTrumpRankOpt playout action =
        assert(playout.CurrentTrick.NumPlays > 0)
        let lowTrumpRankOpt =
            assert(
                Rank.lower
                    (playout.History.LowTakenOpt |> Option.map fst)
                    playout.CurrentTrick.LowTrumpRankOpt
                    = playout.CurrentTrick.LowTrumpRankOpt)
            Rank.lower
                handLowTrumpRankOpt
                playout.CurrentTrick.LowTrumpRankOpt
        match playout.TrumpOpt, playout.CurrentTrick.SuitLedOpt with
            | Some trump, Some suitLed ->
                match action with
                    | PlayTrump rank ->
                        let card = Card(rank, trump)
                        assert
                            (playout
                                |> AbstractPlayout.legalPlays hand
                                |> Seq.contains card)
                        card
                    | PlayTrumpWin ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                assert(card.Suit <> trump
                                    || Some card.Rank >= lowTrumpRankOpt)
                                card.Suit = trump
                                    && card.Rank.GamePoints = 0
                                    && Some card.Rank > lowTrumpRankOpt)
                            |> Seq.maxBy (fun card -> card.Rank)
                    | PlayTrumpLose ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                assert(card.Suit <> trump
                                    || Some card.Rank >= lowTrumpRankOpt)
                                card.Suit = trump
                                    && card.Rank.GamePoints = 0
                                    && Some card.Rank > lowTrumpRankOpt)
                            |> Seq.minBy (fun card -> card.Rank)
                    | FollowSuitWin gamePoints ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Suit = suitLed
                                    && card.Rank.GamePoints = gamePoints)
                            |> Seq.maxBy (fun card -> card.Rank)
                    | FollowSuitLose gamePoints ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Suit = suitLed
                                    && card.Rank.GamePoints = gamePoints)
                            |> Seq.minBy (fun card -> card.Rank)
                    | ContributeTen ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.find (fun card ->
                                card.Suit <> trump
                                    && card.Suit <> suitLed
                                    && card.Rank = Rank.Ten)
                    | ContributeGame ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Suit <> suitLed
                                    && card.Rank <> Rank.Ten
                                    && card.Rank.GamePoints > 0)
                            |> Seq.maxBy (fun card ->
                                card.Rank.GamePoints)
                    | Duck ->
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.where (fun card ->
                                card.Suit <> trump
                                    && card.Suit <> suitLed
                                    && card.Rank.GamePoints = 0)
                            |> Seq.minBy (fun card -> card.Rank)
            | _ -> failwith "Unexpected"

    /// String representation of a follow action.
    let layout =
        [|
            SpanLayout.ofLength 1   // action type
            SpanLayout.ofLength 1   // action detail
        |] |> SpanLayout.combine

    /// String representation of a follow action.
    let copyTo (span : Span<_>) action =
        assert(span.Length = layout.Length)

        let toChar gamePoints =
            assert(gamePoints >= 0 && gamePoints <= 10)
            if gamePoints = 10 then 'T'
            else Char.fromDigit gamePoints

        let slice0 = layout.Slice(0, span)
        let slice1 = layout.Slice(1, span)
        match action with
            | PlayTrump rank ->
                slice0.Fill('T')
                slice1.Fill(rank.Char)
            | PlayTrumpWin ->
                slice0.Fill('W')
                slice1.Fill('x')
            | PlayTrumpLose ->
                slice0.Fill('L')
                slice1.Fill('x')
            | FollowSuitWin gamePoints ->
                slice0.Fill('w')
                slice1.Fill(toChar gamePoints)
            | FollowSuitLose gamePoints ->
                slice0.Fill('l')
                slice1.Fill(toChar gamePoints)
            | ContributeTen ->
                slice0.Fill('N')
                slice1.Fill('.')
            | ContributeGame ->
                slice0.Fill('G')
                slice1.Fill('.')
            | Duck ->
                slice0.Fill('D')
                slice1.Fill('.')
