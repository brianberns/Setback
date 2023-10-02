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

        /// Absolute score of each team in the current game.
        GameScore : AbstractScore

        /// State of random number generator.
        RandomState : uint64   // can't persist entire RNG

        /// Duplicate deal state.
        DuplicateDealState : Option<uint64 (*random state*) * int (*# deals*)>

        /// Current dealer.
        Dealer : Seat
    }

module PersistentState =

    /// Creates initial persistent state.
    let private create () =
        {
            GamesWon = AbstractScore.zero
            GameScore = AbstractScore.zero
            RandomState = Random().State   // start with arbitrary seed
            DuplicateDealState = None
            Dealer = Seat.South
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
    let private incrGamesWon iTeam persState =

            // increment count
        let gamesWon =
            persState.GamesWon + AbstractScore.forTeam iTeam 1

            // update persistent state
        { persState with
            GamesWon = gamesWon
            GameScore = AbstractScore.zero }

    /// Handles the end of a game.
    let private gameOver (surface : JQueryElement) iTeam gamesWon =

            // display banner
        let banner =
            let text = $"{teamNames[iTeam]} win the game!"
            console.log(text)
            ~~HTMLDivElement.Create(innerText = text)
        banner.addClass("banner")
        surface.append(banner)

            // wait for user to click banner
        Promise.create (fun resolve _reject ->
            banner.click(fun () ->
                banner.remove()
                displayGamesWon gamesWon
                resolve ()))

    /// Runs one new game.
    let run surface persState =

        /// Runs one deal.
        let rec loop game persState nDeals =
            async {
                    // display current score of the game
                let dealer = persState.Dealer
                let absScore = Game.absoluteScore dealer game.Score
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])

                    // run a deal
                let rng = Random(persState.RandomState)
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
                let persState' =
                    { persState with
                        GameScore = absScore
                        RandomState = rng.State
                        Dealer = dealer.Next }
                let nDeals' = nDeals + 1
                match winningTeamIdxOpt with

                        // game is over
                    | Some iTeam ->

                            // increment games won
                        let persState'' =
                            incrGamesWon iTeam persState'
                        PersistentState.save persState''

                            // display game result
                        do! gameOver surface iTeam persState''.GamesWon
                            |> Async.AwaitPromise

                        return persState'', nDeals'

                        // run another deal in this game
                    | None ->
                        PersistentState.save persState'
                        let score'' = gameScore |> AbstractScore.shift 1
                        let game' = { game with Score = score'' }
                        return! loop game' persState' nDeals'
            }

            // start a new game
        displayGamesWon persState.GamesWon
        let game =
            let iTeam =
                int persState.Dealer % Setback.numTeams
            let score =
                persState.GameScore
                    |> AbstractScore.shift iTeam
            { Score = score }
        loop game persState 0   // to-do: simplify absolute vs. relative scores
