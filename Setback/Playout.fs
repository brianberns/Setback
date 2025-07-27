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
    }

    /// Trump suit, as determined by first card played.
    member deal.Trump =
        match deal.TrumpOpt with
            | Some trump -> trump
            | None -> failwith "No trump yet"

module Playout =

    /// Creates a new deal.
    let create bidder =
        {
            Bidder = bidder
            TrumpOpt = None
            CurrentTrickOpt = Some (Trick.create bidder)
            CompletedTricks = List.empty
            UnplayedCards = set Card.allCards
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

    let isComplete deal =
        numCardsPlayed deal = Setback.numCardsPerDeal

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
