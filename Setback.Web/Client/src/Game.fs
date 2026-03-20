namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback

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
    let private displayGamesWon (gamesWon : Score) =
        for team in Enum.getValues<Team> do
            let gamesWonElem = gamesWonElems[int team]
            gamesWonElem.text(string gamesWon[team])

    /// Increments the number of games won by the given team.
    let private incrGamesWon team persState =

            // increment count
        let gamesWon =
            persState.GamesWon + Score.create team 1

            // update persistent state
        { persState with GamesWon = gamesWon }

    /// Handles the end of a game.
    let private gameOver (surface : JQueryElement) (team : Team) gamesWon =

            // display banner
        let banner =
            let text = $"{teamNames[int team]} win the game!"
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

    /// Runs the given game.
    let run surface persState =

        /// Runs one deal.
        let rec loop persState =
            async {
                    // display current score of the game
                for team in Enum.getValues<Team> do
                    scoreElems[int team].text(
                        string persState.Game.Score[team])

                    // run a deal
                let! persState = Deal.run surface persState

                    // determine score of this deal
                let dealScore =
                    persState.Game.Deal.ClosedDeal
                        |> ClosedDeal.getDealScore
                do
                    console.log($"E+W make {dealScore[Team.EastWest]} point(s)")
                    console.log($"N+S make {dealScore[Team.NorthSouth]} point(s)")

                    // update game score
                let gameScore = persState.Game.Score + dealScore
                for team in Enum.getValues<Team> do
                    scoreElems[int team].text(string gameScore[team])
                do
                    console.log($"E+W have {gameScore[Team.EastWest]} point(s)")
                    console.log($"N+S have {gameScore[Team.NorthSouth]} point(s)")

                    // is the game over?
                match Game.tryGetWinningTeam persState.Game with

                        // game is over
                    | Some team ->

                            // increment games won
                        let persState = incrGamesWon team persState
                        PersistentState.save persState

                            // display game result
                        do! gameOver surface team persState.GamesWon
                            |> Async.AwaitPromise

                        return persState

                    | None ->

                            // continue current game with new deal
                        let game =
                            Game.startNextDeal Random.Shared persState.Game
                        console.log($"Dealer is {Seat.toString game.Deal.ClosedDeal.Auction.Dealer}")
                        let persState = { persState with Game = game }.Save()

                        return! loop persState
            }

            // start a new game
        displayGamesWon persState.GamesWon
        loop persState
