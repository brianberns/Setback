namespace Setback.Web.Client

open Browser

open Fable.Core
open Fable.SimpleJson

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

/// Session state.
type SessionState =
    {
        /// Number of games won by each team.
        GamesWon : int[]   // SimpleJson doesn't support ImmutableArray

        /// Number of points won by each team in the current game.
        Scores : int[]

        /// State of random number generator.
        RandomState : uint64   // can't persist entire RNG

        /// First (false) or second (true) game of a two-game pair.
        GameParity : bool

        /// Current dealer.
        Dealer : Seat
    }

module SessionState =

    /// Initial state.
    let private initial =
        {
            GamesWon = Array.zeroCreate Setback.numTeams
            Scores = Array.zeroCreate Setback.numTeams
            RandomState = Random().State
            GameParity = false
            Dealer = Seat.South
        }

    /// Local storage key.
    let private key = "SessionState"

    /// Answers the current state.
    let get () =
        WebStorage.localStorage[key]
            |> Option.ofObj
            |> Option.map Json.parseAs<SessionState>
            |> Option.defaultValue initial

    /// Sets the current state.
    let set (sessionState : SessionState) =
        WebStorage.localStorage[key]
            <- Json.serialize sessionState

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

    /// Increments the number of games won by the given team.
    let private incrGamesWon iTeam sessionState =

            // increment count
        let nGames = sessionState.GamesWon[iTeam] + 1
        console.log($"{teamNames[iTeam]} has won {nGames} game(s)")

            // update session state
        let gamesWon =
            ImmutableArray(sessionState.GamesWon)
                .SetItem(iTeam, nGames)
                .ToArray()
        let sessionState' =
            { sessionState with GamesWon = gamesWon }

            // update display
        for iTeam, nGames in Seq.indexed gamesWon do
            gamesWonElems[iTeam].text(string nGames)

        sessionState'

    /// Handles the end of a game.
    let private gameOver (surface : JQueryElement) iTeam sessionState =

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
                sessionState
                    |> incrGamesWon iTeam
                    |> resolve))

    /// Runs one new game.
    let run surface sessionState =

        /// Runs one deal.
        let rec loop (game : Game) sessionState nDeals =
            async {
                    // display current game score
                let dealer = sessionState.Dealer
                let absScore = Game.absoluteScore dealer game.Score
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])

                    // run a deal
                let rng = Random(sessionState.RandomState)
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
                do
                    let absScore = Game.absoluteScore dealer gameScore
                    console.log($"E+W have {absScore[0]} point(s)")
                    console.log($"N+S have {absScore[1]} point(s)")
                    for iTeam = 0 to Setback.numTeams - 1 do
                        scoreElems[iTeam].text(string absScore[iTeam])

                    // is the game over?
                let winningTeamIdxOpt =
                    gameScore |> Game.winningTeamIdxOpt dealer
                let dealer' = dealer.Next
                let nDeals' = nDeals + 1
                match winningTeamIdxOpt with

                        // game is over
                    | Some iTeam ->
                        do! gameOver surface iTeam sessionState
                            |> Async.AwaitPromise
                        return dealer', nDeals'

                        // run another deal
                    | None ->
                        let score'' = gameScore |> AbstractScore.shift 1
                        let game' = { game with Score = score'' }
                        return! loop game' dealer' nDeals'
            }

            // start a new game
        displayGamesWon ()
        loop Game.zero sessionState 0
