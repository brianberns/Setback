namespace Setback

open PlayingCards

/// An action is either a bid (during the auction) or a play
/// (during playout).
type Action = Choice<Bid, Card>

/// All information known to a player about a deal, including
/// information known only to that player.
type InformationSet =
    {
        /// Player.
        Player : Seat

        /// Player's hand.
        Hand : Hand

        /// Public information.
        Deal : ClosedDeal

        /// Game score at the start of the deal. (The score of
        /// the game, not the number of Game points.)
        GameScore : Score

        /// What actions can be taken in this information set?
        LegalActions : Action[]
    }

module InformationSet =

    /// What actions can be taken?
    let private legalActions (hand : Hand) deal =
        match deal.PlayoutOpt with
            | None ->
                Auction.legalBids deal.Auction
                    |> Seq.map Choice1Of2
            | Some playout ->
                Playout.legalPlays hand playout
                    |> Seq.map Choice2Of2

    /// Creates an information set.
    let create player hand deal gameScore =
        assert(ClosedDeal.currentPlayer deal = player)
        assert(ClosedDeal.isComplete deal |> not)
        {
            Player = player
            Hand = hand
            Deal = deal
            GameScore = gameScore
            LegalActions =
                legalActions hand deal
                    |> Seq.toArray
        }

/// Interface for a Setback player.
type Player =
    {
        /// Chooses an action in the given information set.
        Act : InformationSet -> Action
    }
