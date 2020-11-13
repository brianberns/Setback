namespace Setback.Cfrm

open System

open PlayingCards
open Setback

/// Abstract view of cards played previous to the current
/// trick. All team indexes are relative to the dealer's team.
type AbstractPlayoutHistory =
    {
        /// Number of tricks completed in this playout.
        NumTricksCompleted : int

        /// Auction winner attempted to estalish high trump on
        /// first lead?
        HighEstablished : bool

        /// Rank of lowest trump taken so far, if any, with team
        /// that took it.
        LowTakenOpt : Option<Rank * int>

        /// Team that took Jack of trump, if any.
        JackTakenOpt : Option<int>

        /// Game points taken so far, relative to the dealer's team.
        GameScore : AbstractScore

        /// Indexes of players known to be void in trump, relative
        /// to dealer.
        TrumpVoids : Set<int>
    }

module AbstractPlayoutHistory =

    /// Initial state, before the first card of a deal is played.
    let empty =
        {
            NumTricksCompleted = 0
            HighEstablished = false
            LowTakenOpt = None
            JackTakenOpt = None
            GameScore = AbstractScore.zero
            TrumpVoids = Set.empty
        }

    /// Indicates when all tricks in a deal have been completed.
    let isComplete history =
        assert(
            history.NumTricksCompleted >= 0
                && history.NumTricksCompleted <= Setback.numCardsPerHand)
        history.NumTricksCompleted = Setback.numCardsPerHand

    /// Indexes of trick plays, relative to trick leader.
    let private playIndexes =
        [| 0 .. Seat.numSeats - 1 |]

    /// Adds the given completed trick to the given playout history.
    let addTrick trick history =
        assert(trick |> AbstractTrick.isComplete)
        assert(history |> isComplete |> not)

            // first card played on trick
        let firstPlay = trick.Plays.[0]

            // determine index of team that won the trick,
            // relative to the dealer's team
        let iTrickWinnerTeam =
            let iPlayer =
                trick |> AbstractTrick.highPlayerIndex
            iPlayer % Team.numTeams

            // accumulate effect of each play in the trick
        (history, playIndexes)
            ||> Array.fold (fun acc iPlay ->

                    // trump taken?
                let play = trick.Plays.[iPlay]
                let takenOpt =
                    if play.IsTrump then
                        Some (play.Rank, iTrickWinnerTeam)
                    else None

                    // game points taken
                let gameScore =
                    Array.init Team.numTeams (fun it ->
                        if it = iTrickWinnerTeam then
                            play.Rank.GamePoints
                        else 0)
                        |> AbstractScore

                    // void in trump?
                let isVoidTrump =
                    firstPlay.IsTrump && not play.IsTrump

                {
                    NumTricksCompleted =
                        history.NumTricksCompleted + 1

                    HighEstablished =
                        if history.NumTricksCompleted = 0 then
                            assert(not history.HighEstablished)
                            firstPlay.Rank >= Rank.Jack
                        else history.HighEstablished

                    LowTakenOpt =
                        match acc.LowTakenOpt, takenOpt with
                            | None, _ -> takenOpt
                            | _, None -> acc.LowTakenOpt
                            | Some lowTaken, Some taken ->
                                assert(fst lowTaken <> fst taken)
                                min lowTaken taken |> Some

                    JackTakenOpt =
                        let rankOpt, iTeamOpt =
                            Option.unzip takenOpt
                        if rankOpt = Some Rank.Jack then
                            assert(acc.JackTakenOpt.IsNone)
                            iTeamOpt
                        else acc.JackTakenOpt

                    GameScore =
                        acc.GameScore + gameScore

                    TrumpVoids =
                        if isVoidTrump then
                            let iPlayer =
                                (iPlay + trick.LeaderIndex)
                                    % Seat.numSeats
                            acc.TrumpVoids.Add(iPlayer)
                        else acc.TrumpVoids
                })

    /// String representation of an abstract playout history.
    let layout =
        [|
            SpanLayout.ofLength 1   // 0: high established
            SpanLayout.ofLength 1   // 1: low taken
            SpanLayout.ofLength 1   // 2: jack taken
            SpanLayout.ofLength 1   // 3: game delta sign
            SpanLayout.ofLength 1   // 4: trump voids
        |] |> SpanLayout.combine

    /// String representation of an abstract playout history.
    let copyTo (span : Span<_>) handLowTrumpRankOpt trick history =
        assert(history |> isComplete |> not)
        assert(span.Length = layout.Length)

            // high established: 1 char
        let cHigh =
            if history.HighEstablished then 'H'
            else '.'
        layout.Slice(0, span).Fill(cHigh)

            // low taken: 1 char
        let slice = layout.Slice(1, span)
        match history.LowTakenOpt with
            | Some (rank, _) ->
                let lowTrumpRank =
                    let threshold = Rank.Five
                    handLowTrumpRankOpt
                        |> Option.defaultValue threshold
                        |> min threshold
                let cRank =
                    if rank <= lowTrumpRank then rank.Char
                    else 'x'
                slice.Slice(0, 1).Fill(cRank)
            | None ->
                slice.Fill('.')

            // jack taken: 1 char
        let cJack =
            if history.JackTakenOpt.IsSome then 'J'
            else '.'
        layout.Slice(2, span).Fill(cJack)

            // game delta (low-res): 1 char
        let delta =
            let iTeam =
                (trick |> AbstractTrick.currentPlayerIndex)
                    % Team.numTeams
            history.GameScore
                |> AbstractScore.delta iTeam
        let cSign =
            if delta >= 0 then '+' else '-'
        layout.Slice(3, span).Fill(cSign)

            // trump voids: 1 hex char, indexes relative to trick leader
        assert(history.TrumpVoids.Count < Seat.numSeats)   // can never know that all seats are void
        assert(
            history.TrumpVoids
                |> Seq.forall (fun iPlayer ->
                    iPlayer >= 0 && iPlayer < Seat.numSeats))
        let cPlayers =
            let otherPlayerIdxs =
                let iTrickCurPlayer = trick.NumPlays
                history.TrumpVoids
                    |> Seq.map (fun iDealerPlayer ->
                        (iDealerPlayer - trick.LeaderIndex + Seat.numSeats)
                            % Seat.numSeats)
                    |> Seq.where (fun iTrickPlayer ->
                        iTrickPlayer > iTrickCurPlayer)   // subsequent players' voids only
            (0, otherPlayerIdxs)
                ||> Seq.fold (fun acc iPlayer ->
                    assert(iPlayer > 0)
                    acc + (1 <<< iPlayer))
                |> Char.fromHexDigit
        layout.Slice(4, span).Fill(cPlayers)
