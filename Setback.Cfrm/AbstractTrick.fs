namespace Setback.Cfrm

#if !FABLE_COMPILER
open System
open System.Collections.Immutable
#endif

open PlayingCards
open Setback

module Rank =

    /// Answers the lower of two given rank options:
    /// * If both have a value, lower of the two values
    /// * If only one has a value, that value
    /// * If neither has a value, none
    let lower rankOptA rankOptB =
        match rankOptA, rankOptB with
            | Some (rankA : Rank), Some (rankB : Rank) ->
                min rankA rankB |> Some
            | _ ->
                max rankOptA rankOptB

/// Abstract view of the play of a card on a trick.
type AbstractTrickPlay =
    {
        /// Rank of card played.
        Rank : Rank

        /// Card played is trump?
        IsTrump : bool
    }

module AbstractTrickPlay =

    /// Creates a play of the given card.
    let create trump (card : Card) =
        {
            Rank = card.Rank
            IsTrump = (card.Suit = trump)
        }

    /// String representation of a trick play.
    let layout =
        [|
            SpanLayout.ofLength 1   // rank
            SpanLayout.ofLength 1   // suit
        |] |> SpanLayout.combine

    /// String representation of a trick play.
    let copyTo (span : Span<_>) lowTrumpRankOpt play =
        assert(span.Length = layout.Length)

            // rank
        let cRank =
            let isSignificant =

                    // relavant to Game?
                if play.Rank.GamePoints > 0 then
                    true

                    // relevant to Low?
                elif play.IsTrump && play.Rank <= Rank.Five then
                    lowTrumpRankOpt
                        |> Option.map (fun lowTrumpRank ->
                            play.Rank <= lowTrumpRank)
                        |> Option.defaultValue false

                else false
            if isSignificant then play.Rank.Char
            else 'x'
        layout.Slice(0, span).Fill(cRank)

            // suit
        let cSuit =
            if play.IsTrump then 't'
            else 'x'
        layout.Slice(1, span).Fill(cSuit)

/// Abstract view of the high play in a trick.
type AbstractHighPlay =
    {
        /// 0-based index of the current high player, relative to the
        /// dealer (or -1 if no current high play).
        PlayerIndex : int

        /// High play so far.
        Play : Card
    }

module AbstractHighPlay =

    /// Creates a high play for the given player, relative to the dealer.
    let create playerIdx card =
        assert(playerIdx >= 0 && playerIdx < Seat.numSeats)
        {
            PlayerIndex = playerIdx
            Play = card
        }

/// Abstract view of a trick.
type AbstractTrick =
    {
        /// Index of the trick leader relative to the dealer.
        LeaderIndex : int

        /// Plays made on this trick so far. May be empty.
        Plays : ImmutableArray<AbstractTrickPlay>

        /// Suit led on this trick (if it has started).
        SuitLedOpt : Option<Suit>

        /// High play on this trick, relative to the dealer (if
        /// it has started).
        HighPlayOpt : Option<AbstractHighPlay>

        /// Lowest trump seen in playout so far, including this
        /// trick.
        LowTrumpRankOpt : Option<Rank>
    }

    /// Number of plays made on this trick so far.
    member trick.NumPlays =
        trick.Plays.Length

module AbstractTrick =

    /// Creates a trick with the given leader, relative to the
    /// dealer.
    let create lowTrumpRankOpt leaderIdx =
        assert(leaderIdx >= 0 && leaderIdx < Seat.numSeats)
        {
            LeaderIndex = leaderIdx
            Plays = ImmutableArray.Empty
            SuitLedOpt = None
            HighPlayOpt = None
            LowTrumpRankOpt = lowTrumpRankOpt
        }

    /// Indicates whether the given trick is complete.
    let isComplete (trick : AbstractTrick) =
        assert(trick.NumPlays >= 0 && trick.NumPlays <= Seat.numSeats)
        assert(trick.Plays.Length = trick.NumPlays)
        trick.NumPlays = Seat.numSeats

    /// Index of the current player, relative to the dealer.
    let currentPlayerIndex (trick : AbstractTrick) =
        (trick.NumPlays + trick.LeaderIndex) % Seat.numSeats

    /// Adds the given card to the given trick.
    let addPlay trump (card : Card) trick =
        assert(trick |> isComplete |> not)
        {
            trick with

                    // add play to this trick
                Plays =
                    let play =
                        AbstractTrickPlay.create trump card
                    let builder =
                        ImmutableArray.CreateBuilder(trick.NumPlays + 1)
                    builder.AddRange(trick.Plays)
                    builder.Add(play)
                    builder.ToImmutable()

                    // establish suit led on this trick?
                SuitLedOpt =
                    trick.SuitLedOpt
                        |> Option.defaultValue card.Suit
                        |> Some

                    // is this card now taking the trick?
                HighPlayOpt =
                    let isHighPlay =
                        match trick.HighPlayOpt with
                            | None -> true
                            | Some highPlay ->
                                let highCard = highPlay.Play
                                (card.Suit = highCard.Suit
                                    && card.Rank > highCard.Rank)
                                    || (card.Suit = trump
                                        && highCard.Suit <> trump)
                    if isHighPlay then
                        let iPlayer = currentPlayerIndex trick
                        AbstractHighPlay.create iPlayer card |> Some
                    else
                        trick.HighPlayOpt

                    // is this card a new Low candidate?
                LowTrumpRankOpt =
                    if card.Suit = trump then
                        trick.LowTrumpRankOpt
                            |> Option.map (min card.Rank)
                            |> Option.defaultValue card.Rank
                            |> Some
                    else
                        trick.LowTrumpRankOpt
        }

    /// Answers the index of the player who is winning the given trick,
    /// relative to the dealer.
    let highPlayerIndex (trick : AbstractTrick) =
        assert(trick.NumPlays > 0)
        match trick.HighPlayOpt with
            | Some highPlay -> highPlay.PlayerIndex
            | None -> failwith "Unexpected"

    module private Plays =

        /// String representation of trick plays.
        let layout =
            AbstractTrickPlay.layout
                |> Array.replicate (Seat.numSeats - 1)
                |> SpanLayout.combine

        /// String representation of trick plays.
        let copyTo (span : Span<_>) lowTrumpRankOpt (plays : ImmutableArray<_>) =

                // copy each play into its correct position in the span
            for iPlay = 0 to plays.Length - 1 do
                let play = plays.[iPlay]
                let slice = layout.Slice(iPlay, span)
                AbstractTrickPlay.copyTo
                    slice
                    lowTrumpRankOpt
                    play

                // fill remaining portion of span
            span
                .Slice(plays.Length * AbstractTrickPlay.layout.Length)
                .Fill('.')

    /// String representation of a trick.
    let layout =
        [|
            Plays.layout            // cards played
            SpanLayout.ofLength 1   // trick winner so far, relative to trick leader
        |] |> SpanLayout.combine

    /// String representation of a trick.
    let copyTo (span : Span<_>) handLowTrumpRankOpt trick =
        assert(trick |> isComplete |> not)
        assert(span.Length = layout.Length)

            // trick plays
        let slice = layout.Slice(0, span)
        let lowTrumpRankOpt =
            Rank.lower
                trick.LowTrumpRankOpt   // includes low trump rank in previous tricks
                handLowTrumpRankOpt
        Plays.copyTo
            slice
            lowTrumpRankOpt
            trick.Plays

            // trick winner
        let cWinner =
            trick.HighPlayOpt
                |> Option.map (fun highPlay ->
                    let iTrickPlayer =
                        (highPlay.PlayerIndex - trick.LeaderIndex + Seat.numSeats)
                            % Seat.numSeats
                    Char.fromDigit iTrickPlayer)
                |> Option.defaultValue '.'
        layout.Slice(1, span).Fill(cWinner)
