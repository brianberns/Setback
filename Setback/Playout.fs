namespace Setback

open PlayingCards

/// Discards by players from their hands, grouped into tricks.
type Playout =
    {
        /// Seat of player who won the auction.
        Bidder : Seat

        /// Trump suit, as determined by first card played.
        TrumpOpt : Option<Suit>

        /// Current active trick, if play is in progress. No
        /// trick is active during the auction, nor after the
        /// last card of the deal is played.
        CurrentTrickOpt : Option<Trick>

        /// Completed tricks, in reverse chronological order.
        CompletedTricks : List<Trick>

        /// Cards not yet played.
        UnplayedCards : Set<Card>

        /// Suits that players are known to be void in.
        Voids : Set<Seat * Suit>

        HighTrumpTeamOpt : Option<Rank * Team>
        LowTrumpTeamOpt : Option<Rank * Team>
        JackTrumpTeamOpt : Option<Team>
    }

    /// Trump suit, as determined by first card played.
    member this.Trump =
        match this.TrumpOpt with
            | Some trump -> trump
            | None -> failwith "No trump yet"

module Playout =

    /// Creates a new playout.
    let create bidder =
        {
            Bidder = bidder
            TrumpOpt = None
            CurrentTrickOpt = Some (Trick.create bidder)
            CompletedTricks = List.empty
            UnplayedCards = set Card.allCards
            Voids = Set.empty
            HighTrumpTeamOpt = None
            LowTrumpTeamOpt = None
            JackTrumpTeamOpt = None
        }

    /// Number of cards played so far.
    let numCardsPlayed playout =
        let nCompleted =
            playout.CompletedTricks.Length * Seat.numSeats
        let nCurrent =
            playout.CurrentTrickOpt
                |> Option.map (fun trick ->
                    trick.Cards.Length)
                |> Option.defaultValue 0
        nCompleted + nCurrent

    let isComplete playout =
        numCardsPlayed playout = Setback.numCardsPerDeal

    /// Current trick in the given playout.
    let currentTrick playout =
        match playout.CurrentTrickOpt with
            | Some trick -> trick
            | None -> failwith "No current trick"

    /// Current player in the given playout.
    let currentPlayer playout =
        playout
            |> currentTrick
            |> Trick.currentPlayer

    /// What cards can be played from the given hand?
    let legalPlays hand playout =
        let trick = currentTrick playout
        assert(trick.SuitLedOpt.IsNone = trick.Cards.IsEmpty)
        match playout.TrumpOpt, trick.SuitLedOpt with

                // start trick with any card
            | _, None -> hand

                // continue trick by following suit, if possible, or trumping in
            | Some trump, Some suitLed ->
                let followsSuit (card : Card) =
                    card.Suit = suitLed
                if hand |> Seq.exists followsSuit then
                    hand |> Seq.where (fun card ->
                        card.Suit = trump || followsSuit card)
                else
                    hand

            | _ -> failwith "Unexpected"

    /// Is the given player known to be void in the given suit?
    let private isVoid seat suit playout =
        playout.Voids.Contains (seat, suit)

    /// Establishes trump in the given suit, if necessary.
    let private updateTrump suit playout =
        let trump =
            playout.TrumpOpt
                |> Option.defaultValue suit
        { playout with TrumpOpt = Some trump }

    /// Updates deal points in the given trick.
    let private updateDealPoints
        trick takerTeam (playout : Playout) =
        assert(Trick.isComplete trick)
        assert(
            trick.HighPlayOpt
                |> Option.map (fst >> Team.ofSeat)
                = Some takerTeam)

            // get ranks of trump cards in this trick
        let trumpRanks =
            let trump = playout.Trump
            Trick.plays trick
                |> Seq.choose (fun (_, card) ->
                    if card.Suit = trump then
                        Some card.Rank
                    else None)
                |> Seq.toArray

        /// Updates a (Rank, Team) pair.
        let updateTrumpTeam tryFind trumpTeamOpt op =
            let newRankOpt = tryFind trumpRanks
            match trumpTeamOpt, newRankOpt with
                | None, Some newRank ->
                    Some (newRank, takerTeam)
                | Some (oldRank : Rank, _ : Team), Some newRank
                    when op newRank oldRank ->
                    assert(newRank <> oldRank)
                    Some (newRank, takerTeam)
                | trumpTeamOpt, _ -> trumpTeamOpt

            // update High point
        let highTrumpTeamOpt =
            updateTrumpTeam
                Array.tryMax playout.HighTrumpTeamOpt (>)

            // update Low point
        let lowTrumpTeamOpt =
            updateTrumpTeam
                Array.tryMin playout.LowTrumpTeamOpt (<)

            // update Jack point
        let jackTrumpTeamOpt =
            let jackFlag =
                Array.contains Rank.Jack trumpRanks
            if jackFlag then
                assert(playout.JackTrumpTeamOpt.IsNone)
                Some takerTeam
            else playout.JackTrumpTeamOpt

        { playout with
            HighTrumpTeamOpt = highTrumpTeamOpt
            LowTrumpTeamOpt = lowTrumpTeamOpt
            JackTrumpTeamOpt = jackTrumpTeamOpt }

    /// Completes the given trick in the given playout.
    let private completeTrick trick playout =

            // add to completed tricks
        let completedTricks = trick :: playout.CompletedTricks

            // determine trick taker
        let taker =
            match trick.HighPlayOpt with
                | Some (seat, _) -> seat
                | None -> failwith "No trick taker"

            // update deal points
        let playout =
            let takerTeam = Team.ofSeat taker
            updateDealPoints trick takerTeam playout

            // start new trick?
        let newTrickOpt =
            if completedTricks.Length < Setback.numCardsPerHand then
                Some (Trick.create taker)
            else None   // playout is over

        { playout with
            CompletedTricks = completedTricks
            CurrentTrickOpt = newTrickOpt }

    /// Plays the given card on the current trick.
    let private updateCurrentTrick
        (card : Card) (playout : Playout) =

            // play card on the current trick
        let trick, player =
            let trump = playout.Trump
            let trick = currentTrick playout
            let player = Trick.currentPlayer trick
            assert(playout |> isVoid player card.Suit |> not)
            let trick = trick |> Trick.addPlay trump card
            trick, player

            // trick is complete?
        let playout =
            if Trick.isComplete trick then
                completeTrick trick playout
            else
                { playout with
                    CurrentTrickOpt = Some trick }

        trick, player, playout

    /// Removes the given card from play.
    let private updateUnplayedCards card playout =
        assert(playout.UnplayedCards.Contains(card))
        let unplayedCards =
            playout.UnplayedCards.Remove(card)
        { playout with UnplayedCards = unplayedCards }

    /// Updates void suits in the given playout.
    let private updateVoids
        player suit (trick : Trick) (playout : Playout) =
        let voids =
            match trick.SuitLedOpt with
                | Some suitLed ->
                    let trump = playout.Trump
                    if suit = suitLed || suit = trump then
                        assert(
                            suit = trump
                                || playout |> isVoid player suit |> not)
                        playout.Voids
                    else
                        playout.Voids.Add(player, suitLed)
                | None -> failwith "No suit led"
        { playout with Voids = voids }

    /// Plays the given card on the given playout.
    let addPlay (card : Card) playout =
        let trick, player, playout =
            playout
                |> updateTrump card.Suit
                |> updateCurrentTrick card
        playout
            |> updateUnplayedCards card
            |> updateVoids player card.Suit trick

    /// Tricks in the given playout, in chronological order, including
    /// the current trick (if any).
    let tricks playout =
        seq {
            yield! playout.CompletedTricks
                |> List.rev
            match playout.CurrentTrickOpt with
                | Some trick -> yield trick
                | None -> ()
        }

    let toString playout =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        let tricks = tricks playout |> Seq.toArray
        if tricks.Length > 0 then
            writeline ""
            tricks
                |> Seq.iteri (fun iTrick trick ->
                    write (sprintf "%A: " (iTrick + 1))
                    let sTrick =
                        trick
                            |> Trick.plays
                            |> Seq.map (fun (seat, card) ->
                                sprintf "%c:%A" seat.Char card)
                            |> String.concat " "
                    writeline (sprintf "%s" sTrick))

        sb.ToString()
