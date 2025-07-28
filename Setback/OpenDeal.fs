namespace Setback

open PlayingCards

/// An open deal contains each player's hand. This is used for two purposes:
/// * As the central authority when "hosting" a game of Setback
/// * As a predictive mechanism when evaluating a closed deal
type OpenDeal =
    {
        /// Base deal.
        ClosedDeal : ClosedDeal

        /// Each player's unplayed cards.
        UnplayedCardMap : Map<Seat, Set<Card>>   // to-do: Define Hand = Set<Card> instead of Seq<Card>

        /// Highest trump card dealt, if any.
        HighTrumpOpt : Option<Card>

        /// Lowest trump card dealt, if any.
        LowTrumpOpt : Option<Card>

        /// Jack of trump, if any.
        JackTrumpOpt: Option<Card>

        /// Total number of game points (AKQJT) dealt.
        GamePointsTotal : int

        /// Number of Game points taken by each team so far.
        GameScore : Score
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
            HighTrumpOpt = None
            LowTrumpOpt = None
            JackTrumpOpt = None
            GamePointsTotal =
                handMap.Values
                    |> Seq.concat
                    |> Seq.sumBy _.Rank.GamePoints
            GameScore = Score.zero
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

    /// Adds the given bid to the given deal.
    let addBid bid deal =
        { deal with
            ClosedDeal = ClosedDeal.addBid bid deal.ClosedDeal }

    /// Plays the given card on the given deal.
    let addPlay card deal =

            // add card to underlying closed deal
        let closedDeal =
            deal.ClosedDeal
                |> ClosedDeal.addPlay card

            // remove card from play
        let unplayedCardMap =
            let seat = ClosedDeal.currentPlayer deal.ClosedDeal
            let unplayedCards = deal.UnplayedCardMap[seat]
            assert(unplayedCards.Contains(card))
            let unplayedCards = unplayedCards.Remove(card)
            deal.UnplayedCardMap |> Map.add seat unplayedCards

            // determine high, low, and Jack of trump?
        let highTrumpOpt, lowTrumpOpt, jackTrumpOpt =
            assert(deal.HighTrumpOpt.IsSome = deal.LowTrumpOpt.IsSome)
            assert(deal.JackTrumpOpt.IsNone || deal.HighTrumpOpt.IsSome)
            if deal.HighTrumpOpt.IsNone then
                let trumpCards =
                    let trump = card.Suit
                    deal.UnplayedCardMap.Values
                        |> Seq.concat
                        |> Seq.where (fun card ->
                            card.Suit = trump)
                        |> Seq.toArray
                let highTrump = Array.max trumpCards
                let lowTrump = Array.min trumpCards
                let jackTrumpOpt =
                    trumpCards
                        |> Seq.tryFind (fun card ->
                            card.Rank = Rank.Jack)
                Some highTrump,
                Some lowTrump,
                jackTrumpOpt
            else
                deal.HighTrumpOpt,
                deal.LowTrumpOpt,
                deal.JackTrumpOpt

        {
            deal with
                ClosedDeal = closedDeal
                UnplayedCardMap = unplayedCardMap
                HighTrumpOpt = highTrumpOpt
                LowTrumpOpt = lowTrumpOpt
                JackTrumpOpt = jackTrumpOpt
        }

    let toString (deal : OpenDeal) =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        writeline ""
        for seat in Seat.cycle deal.ClosedDeal.Dealer.Next do
            let sHand = deal.UnplayedCardMap[seat] |> Hand.toString
            writeline (sprintf "%-5s: %s" (seat.ToString()) sHand)

        write (ClosedDeal.toString deal.ClosedDeal)

        let dumpScore teams (score : Score) =
            for team in teams do
                sb.AppendFormat("   {0}: {1}\r\n", team, score[team]) |> ignore

        sb.ToString()
