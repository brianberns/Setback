namespace Setback

open PlayingCards
open Setback

/// All information known to a player about a deal,
/// including information known only to that player.
type InformationSet =
    {
        /// Player.
        Player : Seat

        /// Player's hand.
        Hand : Hand

        /// Public information.
        Playout : Playout

        /// What cards can be played in this information set?
        LegalPlays : Card[]
    }

module InformationSet =

    /// Creates an information set.
    let create player hand playout =
        assert(Playout.isComplete playout |> not)
        {
            Player = player
            Hand = hand
            Playout = playout
            LegalPlays =
                Playout.legalPlays hand playout
                    |> Seq.toArray
        }

/// Interface for a Setback player.
type Player =
    {
        /// Chooses a play in the given information set.
        MakePlay : InformationSet -> Card
    }

module OpenDeal =

    /// Answers the current player's information set.
    let currentInfoSet deal =
        let player =
            ClosedDeal.currentPlayer deal.ClosedDeal
        let hand = deal.UnplayedCardMap[player]
        let playout =
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout -> playout
                | None -> failwith "No playout"
        InformationSet.create player hand playout
