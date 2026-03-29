namespace Setback

open System
open PlayingCards

/// A game of Setback.
type Game =
    {
        /// Current (or just completed) deal.
        Deal : OpenDeal

        /// Game score, updated after every deal is complete..
        Score : Score
    }

module Game =

    /// Creates a new deal.
    let private createDeal rng dealer =
        Deck.shuffle rng
            |> OpenDeal.fromDeck dealer

    /// Creates a new game.
    let create rng dealer =
        {
            Deal = createDeal rng dealer
            Score = Score.zero
        }

    /// Answers the current player's information set.
    let currentInfoSet (game : Game) =
        let deal = game.Deal
        let player = OpenDeal.currentPlayer deal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create
            player hand deal.ClosedDeal game.Score

    /// Which team (if any) won the given game.
    let tryGetWinningTeam game =
        Score.tryGetWinningTeam game.Score

    /// Takes the given action in the given game's current deal.
    /// At the end of each deal, the score is updated, and, if
    /// the game is not over, a new deal is created.
    let addAction action (game : Game) =

            // update the current deal
        let deal = OpenDeal.addAction action game.Deal
        let game = { game with Deal = deal }

            // update score at end of deal
        if OpenDeal.isComplete deal then
            let dealScore =
                ClosedDeal.getDealScore deal.ClosedDeal
            { game with Score = game.Score + dealScore }
        else game

    /// Starts a new deal in the given game.
    let startNextDeal rng game =
        assert(OpenDeal.isComplete game.Deal)
        assert(tryGetWinningTeam game |> Option.isNone)
        let dealer =
            Seat.incr 1 game.Deal.ClosedDeal.Dealer
        let deal = createDeal rng dealer
        { game with Deal = deal }

    /// Plays the given game to completion.
    let playGame rng (playerMap : Map<_, _>) game =

        let rec loop game =

                // make one play
            let infoSet = currentInfoSet game
            let action =
                match Seq.tryExactlyOne infoSet.LegalActions with
                    | Some action -> action
                    | None -> playerMap[infoSet.Player].Act infoSet
            let game = addAction action game

                // start another deal?
            if OpenDeal.isComplete game.Deal then
                match tryGetWinningTeam game with
                    | Some team -> team   // game is over
                    | None -> loop (startNextDeal rng game)
            else loop game

        loop game

    /// Generates an infinite sequence of games.
    let generate rng =
        Seq.initInfinite (fun iGame ->
            iGame % Seat.numSeats
                |> enum<Seat>
                |> create rng)

    /// Plays the given number of games.
    let playGames rng inParallel numGames playFun =
        let map =
            if inParallel then Array.Parallel.map
            else Array.map
        generate rng
            |> Seq.take numGames
            |> Seq.toArray
            |> map playFun
