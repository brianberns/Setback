namespace Setback

open PlayingCards

/// Possible points(*) available in a Setback deal.
///
/// *The terminology here gets quite confusing. We use the term "deal
/// point" to distinguish these points from "game points". Whichever team
/// gets the most game points in a deal wins the "Game" deal point for
/// that deal.
type DealPoint =
    | High = 0   // highest dealt trump
    | Low  = 1   // lowest dealt trump
    | Jack = 2   // jack of trump (if dealt)
    | Game = 3   // AKQJT of all suits

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
    }

    /// Trump suit, as determined by first card played.
    member deal.Trump =
        match deal.TrumpOpt with
            | Some trump -> trump
            | None -> failwith "No trump yet"

module ClosedDeal =

    /// Creates a new deal.
    let create bidder =
        {
            Bidder = bidder
            TrumpOpt = None
            CurrentTrickOpt = None
            CompletedTricks = List.empty
            UnplayedCards = Set.empty
            Voids = Set.empty
        }

    /// Number of cards played so far.
    let numCardsPlayed deal =
        let nCompleted =
            deal.CompletedTricks.Length * Seat.numSeats
        let nCurrent =
            deal.CurrentTrickOpt
                |> Option.map (fun trick ->
                    trick.Cards.Length)
                |> Option.defaultValue 0
        nCompleted + nCurrent

    /// Current trick in the given deal.
    let currentTrick deal =
        match deal.CurrentTrickOpt with
            | Some trick -> trick
            | None -> failwith "No current trick"

    /// Current player in the given deal.
    let currentPlayer deal =
        deal
            |> currentTrick
            |> Trick.currentPlayer
        (*
        deal.Tricks
            |> List.tryHead
            |> Option.map (fun trick ->

                    // winner of previous trick leads
                if Trick.isComplete trick then
                    None, trick.WinnerSeat

                    // current trick continues
                else
                    let prevSeat, _ = trick.Plays.Head
                    Some trick, prevSeat.Next)
            |> Option.defaultWith (fun () ->

                    // auction winner leads first trick
                None, deal.HighBidderOpt.Value)
        *)

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

    /// Answers a new deal with the next player's given discard.
    let addPlay (card : Card) deal =

            // determine card
        let trump =
            deal.TrumpOpt
                |> Option.defaultValue card.Suit

            // play card on current trick
        let updatedTrick, player =
            let curTrick = deal |> currentTrick
            let player = curTrick |> Trick.currentPlayer
            assert(deal |> isVoid player card.Suit |> not)
            let updatedTrick = curTrick |> Trick.addPlay trump card
            updatedTrick, player

            // complete trick?
        let curTrickOpt, completedTricks =
            if updatedTrick |> Trick.isComplete then
                let taker =
                    match updatedTrick.HighPlayOpt with
                        | Some (seat, _) -> seat
                        | None -> failwith "Unexpected"
                let tricks = updatedTrick :: deal.CompletedTricks
                let curTrickOpt =
                    if tricks.Length < Setback.numCardsPerHand then
                        taker |> Trick.create |> Some
                    else None
                curTrickOpt, tricks
            else
                Some updatedTrick, deal.CompletedTricks

            // remove from unplayed cards
        let unplayedCards =
            assert(deal.UnplayedCards.Contains(card))
            deal.UnplayedCards.Remove(card)

            // player is void in suit led?
        let voids =
            match updatedTrick.SuitLedOpt with
                | Some suitLed ->
                    if card.Suit = suitLed || card.Suit = trump then
                        assert(
                            card.Suit = trump
                                || deal |> isVoid player card.Suit |> not)
                        deal.Voids
                    else
                        deal.Voids.Add(player, suitLed)
                | None -> failwith "Unexpected"

        {
            deal with
                CurrentTrickOpt = curTrickOpt
                CompletedTricks = completedTricks
                UnplayedCards = unplayedCards
                Voids = voids
        }

    /// Answers the points won by each team in this deal.
    let getOutcome (deal : ClosedDeal) =

            // find all played trump cards
        let fullTricks =
            deal.Tricks
                |> Seq.where (fun trick ->
                    trick.NumPlays = Seat.numSeats)
        let winner (trick : Trick) =
            deal.TeamMap[trick.Winner |> fst |> int]
        let trumpPairs =
            fullTricks
                |> Seq.collect (fun trick ->
                    trick.Plays
                        |> Seq.map (fun (_, card) ->
                            winner trick, card)
                        |> Seq.where (fun (_, card) ->
                            card.Suit = deal.Trump))
                |> Seq.toArray

            // which team won high/low trump?
        let highTeamOpt =
            if trumpPairs.Length > 0 then
                Some (trumpPairs |> Seq.maxBy snd |> fst)
            else
                None
        let lowTeamOpt =
            if trumpPairs.Length > 0 then
                Some (trumpPairs |> Seq.minBy snd |> fst)
            else
                None

            // which team won Jack of trump, if any?
        let jackTeamOpt =
            trumpPairs
                |> Seq.tryFind (fun (team, card) -> card.Rank = Rank.Jack)
                |> Option.map fst

            // did one team gather the most game points?
        let gameMap =
            fullTricks
                |> Seq.collect (fun trick ->
                    trick.Plays
                        |> Seq.map (fun (_, card) ->
                            trick, card))
                |> Seq.map (fun (trick, card) ->
                    winner trick, card.Rank.GamePoints)
                |> Seq.groupBy fst
                |> Seq.map (fun (team, pairs) ->
                    let count = pairs |> Seq.sumBy snd
                    team, count)
                |> Seq.toArray
        let maxCount =
            if gameMap.Length > 0 then
                gameMap
                    |> Seq.map snd
                    |> Seq.max
            else
                0
        let maxTeams =
            gameMap
                |> Seq.where (fun (_, count) ->
                    count = maxCount)
                |> Seq.map fst
                |> Seq.toList
        let gameTeamOpt =
            match maxTeams with   // point not awarded in case of tie
                | maxTeam :: [] -> Some maxTeam
                | _ -> None

        Map.empty
            |> Map.add DealPoint.High highTeamOpt
            |> Map.add DealPoint.Low lowTeamOpt
            |> Map.add DealPoint.Jack jackTeamOpt
            |> Map.add DealPoint.Game gameTeamOpt

    /// Answers the number of points won by the given team in the given deal.
    let getOutcomePoints team deal =
        deal |> getOutcome
            |> Seq.where (fun (KeyValue(_, teamOpt)) -> teamOpt = Some team)
            |> Seq.length

    let toString deal =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        if not this.Auction.IsEmpty then
            writeline ""
            for (seat, bid) in List.rev this.Auction do
                writeline (sprintf "%-5s: %A" (seat.ToString()) bid)

        if not this.Tricks.IsEmpty then
            writeline ""
            this.Tricks
                |> List.rev
                |> List.iteri (fun iTrick trick ->
                    write (sprintf "%A: " (iTrick + 1))
                    let sTrick =
                        trick
                            |> Trick.plays
                            |> Seq.map (fun (seat, card) ->
                                sprintf "%c:%A" seat.Char card)
                            |> String.concat " "
                    writeline (sprintf "%s" sTrick))

        sb.ToString()

    /// Tricks in the given deal, in chronological order, including the
    /// current trick (if any).
    let tricks deal =
        seq {
            yield! deal.CompletedTricks
                |> List.rev
            match deal.CurrentTrickOpt with
                | Some trick -> yield trick
                | None -> ()
        }
