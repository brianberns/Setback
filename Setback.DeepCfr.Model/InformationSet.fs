namespace Setback

open PlayingCards
open Setback

/// An action is either a bid (during the auction) or
/// a play (during playout).
type Action =
    | Bid of Bid
    | Play of Card

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
    let private legalActions hand deal =
        match deal.PlayoutOpt with
            | Some playout ->
                playout
                    |> Playout.legalPlays hand
                    |> Seq.map Play
                    |> Seq.toArray
            | None ->
                deal.Auction
                    |> Auction.legalBids
                    |> Seq.map Bid
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
        /// Takes an action in the given information set.
        Act : InformationSet -> Action
    }

module OpenDeal =

    /// Answers the current player's information set.
    let currentInfoSet deal =
        let player =
            ClosedDeal.currentPlayer deal.ClosedDeal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create player hand deal.ClosedDeal

    /// Adds the given action to the given deal.
    let addAction action deal =
        match action with
            | Bid bid -> OpenDeal.addBid bid deal
            | Play card -> OpenDeal.addPlay card deal
