namespace Setback

open PlayingCards
open Setback

/// An action is either a bid (during the auction) or
/// a play (during playout).
type Action =
    | BidAction of Bid
    | PlayAction of Card

/// All information known to a player about a deal,
/// including information known only to that player.
type InformationSet =
    {
        /// Player.
        Player : Seat

        /// Player's hand.
        Hand : Hand

        /// Public information.
        Deal : ClosedDeal

        /// What actions can be taken in this information set?
        LegalActions : Action[]
    }

module InformationSet =

    /// What actions can be taken?
    let private legalActions (hand : Hand) deal =
        match deal.PlayoutOpt with
            | Some playout ->
                Playout.legalPlays hand playout
                    |> Seq.map PlayAction
            | None ->
                if Auction.isComplete deal.Auction then
                    Seq.map PlayAction hand   // lead any card in hand
                else
                    Auction.legalBids deal.Auction
                        |> Seq.map BidAction
            |> Seq.toArray

    /// Creates an information set.
    let create player hand deal =
        assert(ClosedDeal.isComplete deal |> not)
        {
            Player = player
            Hand = hand
            Deal = deal
            LegalActions = legalActions hand deal
        }

/// Interface for a Setback player.
type Player =
    {
        /// Chooses a bid in the given information set.
        MakeBid : InformationSet -> Bid

        /// Chooses a play in the given information set.
        MakePlay : InformationSet -> Card
    }

module OpenDeal =

    /// Answers the current player's information set.
    let currentInfoSet deal =
        let player =
            ClosedDeal.currentPlayer deal.ClosedDeal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create
            player hand deal.ClosedDeal
