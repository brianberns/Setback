namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

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
            let gamesWonElem = gamesWonElems[iTeam]
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
        let rec loop persState game nDeals =
            async {
                    // display current score of the game
                let dealer = persState.Dealer
                let absScore = Game.absoluteScore dealer game.Score
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])

                    // run a deal
                let rng = Random(persState.RandomState)
                let! persState = Deal.run surface persState game.Score

                    // determine score of this deal
                let dealScore =
                    persState.Deal
                        |> AbstractOpenDeal.dealScore
                do
                    let absScore = Game.absoluteScore dealer dealScore
                    console.log($"E+W make {absScore[0]} point(s)")
                    console.log($"N+S make {absScore[1]} point(s)")

                    // update game score
                let gameScore = game.Score + dealScore
                let absScore = Game.absoluteScore dealer gameScore
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string absScore[iTeam])
                do
                    console.log($"E+W have {absScore[0]} point(s)")
                    console.log($"N+S have {absScore[1]} point(s)")

                    // is the game over?
                let winningTeamIdxOpt =
                    gameScore |> Game.winningTeamIdxOpt dealer
                let persState' =
                    { persState with
                        GameScore = absScore
                        RandomState = rng.State
                        Dealer = dealer.Next
                        DealOpt = None }
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
                        return! loop persState' game' nDeals'
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
        loop persState game 0   // to-do: simplify absolute vs. relative scores
