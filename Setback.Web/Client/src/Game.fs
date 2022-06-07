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

    let private teamNames =
        [|
            "East + West"
            "North + South"
        |]

    let private scoreElems =
        [|
            "#ewScore"
            "#nsScore"
        |] |> Array.map (~~)

    let private gamesWonKeys = [| "ewGamesWon"; "nsGamesWon" |]
    let private gamesWonElems =
        [|
            "#ewGamesWon"
            "#nsGamesWon"
        |] |> Array.map (~~)

    let private getNumGamesWon iTeam =
        let key = gamesWonKeys.[iTeam]
        WebStorage.localStorage.[key]
            |> Option.ofObj
            |> Option.map int
            |> Option.defaultValue 0

    let private setNumGamesWon iTeam (nGames : int) =
        let key = gamesWonKeys.[iTeam]
        WebStorage.localStorage.[key] <- string nGames

    let private displayGamesWon () =
        for iTeam = 0 to Setback.numTeams - 1 do
            let gamesWonElem = gamesWonElems.[iTeam]
            let nGames = getNumGamesWon iTeam
            gamesWonElem.text(string nGames)

    /// Increments the number of games won by the given team.
    let private incrGamesWon iTeam =

            // update previous count from storage
        let nGames = (getNumGamesWon iTeam) + 1
        console.log($"{teamNames.[iTeam]} has won {nGames} game(s)")
        setNumGamesWon iTeam nGames

            // update display
        displayGamesWon ()

    /// Handles the end of a game.
    let private gameOver (surface : JQueryElement) iTeam =

            // display banner
        let banner =
            let text = $"{teamNames.[iTeam]} wins the game!"
            console.log(text)
            ~~HTMLDivElement.Create(innerText = text)
        banner.addClass("banner")
        surface.append(banner)

            // wait for user to click banner
        Promise.create (fun resolve _reject ->
            banner.click(fun () ->
                banner.remove()
                incrGamesWon iTeam
                resolve ()))

    /// Runs one new game.
    let run surface rng dealer =

        /// Runs one deal.
        let rec loop (game : Game) dealer nDeals =
            async {
                    // display current game score
                let absScore = Game.absoluteScore dealer game.Score
                for iTeam = 0 to Setback.numTeams - 1 do
                    scoreElems.[iTeam].text(string absScore.[iTeam])

                    // run a deal
                let! deal = Deal.run surface rng dealer game.Score

                    // determine score of this deal
                let dealScore =
                    deal |> AbstractOpenDeal.dealScore
                do
                    let absScore = Game.absoluteScore dealer dealScore
                    console.log($"E+W make {absScore.[0]} point(s)")
                    console.log($"N+S make {absScore.[1]} point(s)")

                    // update game score
                let gameScore = game.Score + dealScore
                do
                    let absScore = Game.absoluteScore dealer gameScore
                    console.log($"E+W have {absScore.[0]} point(s)")
                    console.log($"N+S have {absScore.[1]} point(s)")
                    for iTeam = 0 to Setback.numTeams - 1 do
                        scoreElems.[iTeam].text(string absScore.[iTeam])

                    // is the game over?
                let winningTeamIdxOpt =
                    gameScore |> Game.winningTeamIdxOpt dealer
                let dealer' = dealer.Next
                let nDeals' = nDeals + 1
                match winningTeamIdxOpt with

                        // game is over
                    | Some iTeam ->
                        do! gameOver surface iTeam
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
        loop Game.zero dealer 0
