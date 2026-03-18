namespace Setback.Web.Client

open Browser
open Fable.SimpleJson
open Setback

module Random =

    /// Shared RNG.
    let Shared = System.Random()

/// Persistent state.
type PersistentState =
    {
        /// Structure version number.
        VersionNum : int

        /// Number of games won by each team.
        GamesWon : Score

        /// Current game.
        Game : Game
    }

module PersistentState =

    /// Initial persistent state.
    let private initial =
        {
            VersionNum = 1   // Deep CFR conversion
            GamesWon = Score.zero
            Game = Game.create Random.Shared Seat.South
        }

    /// Local storage keys.
    let private key = "Setback"

    /// Saves the given state.
    let save (persState : PersistentState) =
        WebStorage.localStorage[key]
            <- Json.serialize persState

    /// Answers the current state.
    let get () =
        let json = WebStorage.localStorage[key]
        if isNull json then
            let persState = initial
            save persState
            persState
        else
            Json.parseAs<PersistentState>(json)

type PersistentState with

    /// Saves this state.
    member persState.Save() =
        PersistentState.save persState
        persState
