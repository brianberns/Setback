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
        let persState =
            if isNull json then initial
            else
                match Json.tryParseAs<PersistentState>(json) with
                    | Ok persState -> persState
                    | Error _ ->
                        match Json.tryParseAs<{| GamesWon : {| AbstractScore : int[] |} |}>(json) with
                            | Ok oldState ->
                                { initial with GamesWon = Score.ofPoints oldState.GamesWon.AbstractScore }
                            | Error _ -> initial
        save persState
        persState

type PersistentState with

    /// Saves this state.
    member persState.Save() =
        PersistentState.save persState
        persState
