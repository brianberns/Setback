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
        let rec loop persState nDeals =
            async {
                    // display current score of the game
                let dealer = persState.Dealer
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(
                        string persState.GameScore[iTeam])

                    // run a deal
                let! persState = Deal.run surface persState

                    // determine score of this deal
                let dealScore =
                    persState.Deal
                        |> AbstractOpenDeal.dealScore
                        |> Game.absoluteScore dealer
                do
                    console.log($"E+W make {dealScore[0]} point(s)")
                    console.log($"N+S make {dealScore[1]} point(s)")

                    // update game score
                let gameScore = persState.GameScore + dealScore
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems[iTeam].text(string gameScore[iTeam])
                do
                    console.log($"E+W have {gameScore[0]} point(s)")
                    console.log($"N+S have {gameScore[1]} point(s)")

                    // is the game over?
                let winningTeamIdxOpt =
                    
                    BootstrapGameState.winningTeamOpt gameScore
                let persState' =
                    { persState with
                        GameScore = gameScore
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
                        return! loop persState' nDeals'
            }

            // start a new game
        displayGamesWon persState.GamesWon
        loop persState 0
