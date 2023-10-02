namespace Setback.Web.Client

open Browser

open Fable.Core
open Fable.SimpleJson

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

/// Persistent state.
type PersistentState =
    {
        /// Number of games won by each team.
        GamesWon : AbstractScore

        /// Number of points won by each team in the current game.
        Scores : AbstractScore

        /// State of random number generator.
        RandomState : uint64   // can't persist entire RNG

        /// Duplicate deal state.
        DuplicateDealState : Option<uint64 (*random state*) * int (*# deals*)>

        /// Current dealer.
        Dealer : Seat
    }

module PersistentState =

    /// Initial state.
    let private initial =
        {
            GamesWon = AbstractScore.zero
            Scores = AbstractScore.zero
            RandomState = Random().State
            DuplicateDealState = None
            Dealer = Seat.South
        }

    /// Local storage key.
    let private key = "PersistentState"

    /// Answers the current state.
    let get () =
        WebStorage.localStorage[key]
            |> Option.ofObj
            |> Option.map Json.parseAs<PersistentState>
            |> Option.defaultValue initial

    /// Saves the given state.
    let save (persistentState : PersistentState) =
        WebStorage.localStorage[key]
            <- Json.serialize persistentState

[<AutoOpen>]
module PersitentStateExt =

    /// Saves the given state.
    let (!) persistentState =
        PersistentState.save persistentState
        persistentState

// To-do:
// * Split GameView into separate file
// * Improve Cfrm.Game module to work with actual games
module Game =

    /// Team display names.
    let private teamNames =
        [|
            "East + West"
            "North + South"
        |]

    /// HTML elements holding current score.
    let private scoreElems =
        [|
            "#ewScore"
            "#nsScore"
        |] |> Array.map (~~)

    /// HTML elements holding number of games won.
    let private gamesWonElems =
        [|
            "#ewGamesWon"
            "#nsGamesWon"
        |] |> Array.map (~~)

    /// Displays the number of games won by all teams.
    let private displayGamesWon (gamesWon : AbstractScore) =
        for iTeam = 0 to Setback.numTeams - 1 do
            let gamesWonElem = gamesWonElems.[iTeam]
            gamesWonElem.text(string gamesWon[iTeam])

    /// Increments the number of games won by the given team.
    let private incrGamesWon iTeam persistentState =

            // increment count
        let gamesWon =
            persistentState.GamesWon + AbstractScore.forTeam iTeam 1
        console.log($"{teamNames[iTeam]} has won {gamesWon[iTeam]} game(s)")

            // update display
        displayGamesWon gamesWon

            // update persistent state
        { persistentState with GamesWon = gamesWon }

    /// Handles the end of a game.
    let private gameOver (surface : JQueryElement) iTeam persistentState =

            // display banner
        let banner =
            let text = $"{teamNames[iTeam]} wins the game!"
            console.log(text)
            ~~HTMLDivElement.Create(innerText = text)
        banner.addClass("banner")
        surface.append(banner)

            // wait for user to click banner
        Promise.create (fun resolve _reject ->
            banner.click(fun () ->
                banner.remove()
                persistentState
                    |> incrGamesWon iTeam
                    |> resolve))

    /// Runs one new game.
    let run surface persistentState =

        /// Runs one deal.
        let rec loop game persistentState nDeals =
            async {
                    // display current score of the game
                let dealer = persistentState.Dealer
                let absScore = Game.absoluteScore dealer game.Score
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])

                    // run a deal
                let rng = Random(persistentState.RandomState)
                let! deal = Deal.run surface rng dealer game.Score

                    // determine score of this deal
                let dealScore =
                    deal |> AbstractOpenDeal.dealScore
                do
                    let absScore = Game.absoluteScore dealer dealScore
                    console.log($"E+W make {absScore[0]} point(s)")
                    console.log($"N+S make {absScore[1]} point(s)")

                    // update game score
                let gameScore = game.Score + dealScore
                let absScore = Game.absoluteScore dealer gameScore
                console.log($"E+W have {absScore[0]} point(s)")
                console.log($"N+S have {absScore[1]} point(s)")
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])

                    // is the game over?
                let winningTeamIdxOpt =
                    gameScore |> Game.winningTeamIdxOpt dealer
                let persistentState' =
                    !{ persistentState with
                        Scores = absScore
                        RandomState = rng.State
                        Dealer = dealer.Next }
                let nDeals' = nDeals + 1
                match winningTeamIdxOpt with

                        // game is over
                    | Some iTeam ->
                        let! persistentState'' =
                            gameOver surface iTeam persistentState'
                                |> Async.AwaitPromise
                        return !persistentState'', nDeals'

                        // run another deal
                    | None ->
                        let score'' = gameScore |> AbstractScore.shift 1
                        let game' = { game with Score = score'' }
                        return! loop game' persistentState' nDeals'
            }

            // start a new game
        displayGamesWon persistentState.GamesWon
        loop Game.zero persistentState 0
