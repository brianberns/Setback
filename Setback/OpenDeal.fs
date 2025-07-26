namespace Setback

open PlayingCards
open System.Collections.Immutable

/// An open deal contains each player's hand. This is used for two purposes:
/// * As the central authority when "hosting" a game of Setback
/// * As a predictive mechanism when evaluating a closed deal
[<StructuredFormatDisplay("{String}")>]
type OpenDeal =
    {
        /// Base deal.
        ClosedDeal : ClosedDeal

        /// Each player's unplayed cards.
        UnplayedCardMap : Map<Seat, Hand>
    }

module OpenDeal =

    /// Number of cards dealt to each player.
    let numCardsPerHand = 6

    /// Creates a deal from the given hands.
    let fromHands dealer handMap =
        assert(
            let nCards =
                handMap
                    |> Map.toSeq
                    |> Seq.collect snd
                    |> Seq.distinct
                    |> Seq.length
            nCards = Setback.numCardsPerDeal)
        {
            ClosedDeal = ClosedDeal.create dealer
            UnplayedCardMap = handMap
        }

    /// Deals cards from the given deck to each player.
    let fromDeck dealer deck =
        deck.Cards
            |> Seq.indexed
            |> Seq.groupBy (fun (iCard, _) ->
                let n = (iCard + 1) % Seat.numSeats
                dealer |> Seat.incr n)
            |> Seq.map (fun (seat, group) ->
                let cards =
                    group
                        |> Seq.map snd
                        |> set
                seat, cards)
            |> Map
            |> fromHands dealer

    /// Total number of deal points available in the given deal (either 3 or 4).
    let totalDealPoints (deal : OpenDeal) =
        if deal.JackTrumpOpt.IsSome then 4 else 3

    /// Answers a new deal with the next player's given bid.
    let addBid bid deal =
        { deal with
            ClosedDeal = ClosedDeal.addBid bid deal.ClosedDeal }

    /// Answers the unplayed cards in the given player's hand.
    let unplayedCards seat deal =
        deal.Hands[int seat]
            |> Seq.where (fun card ->
                not (deal.CardsPlayed[Card.toIndex card]))

    /// Answers the number of cards played so far.
    let numCardsPlayed (deal : OpenDeal) =
        deal.Tricks |> Seq.sumBy (fun trick -> trick.NumPlays)

    /// What cards is the current player allowed to play?
    let legalPlays (deal : OpenDeal) =

            // get unplayed cards in player's hand
        let trickOpt, seat = deal.NextPlayer
        let cards = deal |> unplayedCards seat

        match trickOpt with

                // continue current trick
            | Some trick ->
                let isTrump (card : Card) =
                    card.Suit = deal.Trump
                let isFollowSuit (card : Card) =
                    card.Suit = trick.SuitLed
                if cards |> Seq.exists isFollowSuit then                                     // player can follow suit?
                    if deal.Trump = trick.SuitLed then
                        cards |> Seq.where (fun card -> isTrump card)                        // unroll for max performance
                    else
                        cards |> Seq.where (fun card -> isTrump card || isFollowSuit card)   // player can always trump in
                else
                    cards

                // start a new trick
            | None -> cards


    let toString deal =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        writeline ""
        for seat in Seat.cycle deal.Dealer.Next do
            let sHand = deal.Hands[int seat] |> Hand.toString
            writeline (sprintf "%-5s: %s" (seat.ToString()) sHand)

        write deal.ClosedDeal.String

        let dumpScore teams (score : Score) =
            for team in teams do
                sb.AppendFormat("   {0}: {1}\r\n", team, score[team]) |> ignore

        if not deal.Tricks.IsEmpty then
            writeline ""
            writeline "Game points:"
            dumpScore deal.Teams deal.GamePoints
            writeline ""
            writeline "Deal points:"
            dumpScore deal.Teams deal.DealPoints

        sb.ToString()
