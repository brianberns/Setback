namespace Setback.Web.Client

open System

open Browser

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

module Deal =

    /// Runs the auction of the given deal.
    let private auction surface dealer score deal =

        /// Creates bid chooser.
        let chooseBid handler legalBids =
            let chooser = BidChooser.create legalBids handler
            surface.Element.append(chooser)

            // get bid animation for each seat
        let auctionMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let animBid =
                        AuctionView.bidAnim surface seat
                    seat, animBid)
                |> Map

            // run the auction
        Auction.run dealer score deal chooseBid auctionMap

    /// Runs the playout of the given deal.
    let private playout surface dealer deal handViews =

            // get animations for each seat
        let playoutMap =
            handViews
                |> Seq.map (fun (seat : Seat, handView) ->

                    let animCardPlay =
                        let anim =
                            if seat.IsUser then OpenHandView.playAnim
                            else ClosedHandView.playAnim
                        anim surface seat handView

                    let animTrickFinish =
                        TrickView.finishAnim surface

                    seat, (handView, animCardPlay, animTrickFinish))
                |> Map

            // run the playout
        Playout.play dealer deal playoutMap

    /// Runs one new deal.
    let run surface rng dealer score cont =
        promise {

                // create random deal
            console.log($"Dealer is {Seat.toString dealer}")
            let deal =
                Deck.shuffle rng
                    |> AbstractOpenDeal.fromDeck dealer

                // animate dealing the cards
            let! seatViews = DealView.start surface dealer deal

                // run the auction and then playout
            auction surface dealer score deal (fun deal' ->

                    // force cleanup after all-pass auction
                if deal'.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                    for (_, handView) in seatViews do
                        for (cardView : CardView) in handView do
                            cardView.remove()
                    cont deal'

                else
                    playout surface dealer deal' seatViews cont)

        } |> ignore

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
    let private gameOver surface iTeam cont =

            // display banner
        let banner =
            let text = $"{teamNames.[iTeam]} wins the game!"
            console.log(text)
            ~~HTMLDivElement.Create(innerText = text)
        banner.addClass("banner")
        banner.click(fun () ->
            banner.remove()
            incrGamesWon iTeam
            cont ())
        surface.Element.append(banner)

    /// Runs one new game.
    let run surface rng dealer cont =

        /// Runs one deal.
        let rec loop (game : Game) dealer =

                // display current game score
            let absScore = Game.absoluteScore dealer game.Score
            for iTeam = 0 to Setback.numTeams - 1 do
                scoreElems.[iTeam].text(string absScore.[iTeam])

                // run a deal
            Deal.run surface rng dealer game.Score
                (update dealer game)

        /// Updates game state after a deal is complete.
        and update dealer game deal =
            promise {

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
                match winningTeamIdxOpt with

                        // game is over
                    | Some iTeam ->
                        gameOver surface iTeam (fun () ->
                            cont dealer')

                        // run another deal
                    | None ->
                        let game' =
                            let score'' = gameScore |> AbstractScore.shift 1
                            { game with Score = score'' }
                        loop game' dealer'
            } |> ignore

        displayGamesWon ()
        loop Game.zero dealer

module Session =

    /// Runs a new session.
    let run surface rng dealer =
        let rec loop dealer =
            Game.run surface rng dealer loop
        loop dealer

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = CardSurface.init "#surface"
        let rng = Random()
        Session.run surface rng Seat.South)
