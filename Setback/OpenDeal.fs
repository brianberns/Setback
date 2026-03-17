namespace Setback

open System
open PlayingCards

/// An open deal contains each player's hand. This is used for two purposes:
/// * As the central authority when "hosting" a game of Setback
/// * As a predictive mechanism when evaluating a closed deal
type OpenDeal =
    {
        /// Base deal.
        ClosedDeal : ClosedDeal

        /// Each player's unplayed cards.
        UnplayedCardMap : Map<Seat, Hand>
    }

module OpenDeal =

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
                        |> Seq.take Setback.numCardsPerHand
                        |> set
                seat, cards)
            |> Map
            |> fromHands dealer

    /// Indicates whether the given deal has finished.
    let isComplete deal =
        ClosedDeal.isComplete deal.ClosedDeal

    /// Current player in the given deal.
    let currentPlayer deal = 
        ClosedDeal.currentPlayer deal.ClosedDeal

    /// Answers the current player's information set.
    let currentInfoSet deal =
        let player = currentPlayer deal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create player hand deal.ClosedDeal

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
            let seat = currentPlayer deal
            let unplayedCards = deal.UnplayedCardMap[seat]
            assert(unplayedCards.Contains(card))
            let unplayedCards = unplayedCards.Remove(card)
            deal.UnplayedCardMap |> Map.add seat unplayedCards

        {
            deal with
                ClosedDeal = closedDeal
                UnplayedCardMap = unplayedCardMap
        }

    /// Takes the given action in the given deal.
    let addAction action deal =
        match action with
            | Choice1Of2 bid -> addBid bid deal
            | Choice2Of2 card -> addPlay card deal
