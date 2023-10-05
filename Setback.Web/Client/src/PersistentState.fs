namespace Setback.Web.Client

open Browser

open Fable.SimpleJson

open PlayingCards
open Setback
open Setback.Cfrm

/// Persistent state.
type PersistentState =
    {
        /// Number of games won by each team.
        GamesWon : AbstractScore

        /// Absolute score of each team in the current game.
        GameScore : AbstractScore

        /// State of random number generator.
        RandomState : uint64   // can't persist entire RNG

        /// Current dealer.
        Dealer : Seat

        /// Current deal, if any.
        DealOpt : Option<AbstractOpenDeal>
    }

module PersistentState =

    /// Creates initial persistent state.
    let private create () =
        {
            GamesWon = AbstractScore.zero
            GameScore = AbstractScore.zero
            RandomState = Random().State   // start with arbitrary seed
            Dealer = Seat.South
            DealOpt = None
        }

    /// Local storage key.
    let private key = "PersistentState"

    /// Saves the given state.
    let save (persState : PersistentState) =
        WebStorage.localStorage[key]
            <- Json.serialize persState

    /// Answers the current state.
    let get () =
        let json = WebStorage.localStorage[key] 
        if isNull json then
            let persState = create ()
            save persState
            persState
        else
            Json.parseAs<PersistentState>(json)

type PersistentState with

    /// Saves this state.
    member persState.Save() =
        PersistentState.save persState
        persState
