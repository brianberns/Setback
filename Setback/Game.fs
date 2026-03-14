namespace Setback

open PlayingCards

/// A game of Hearts.
type Game =
    {
        /// Current deal.
        Deal : OpenDeal

        /// Game score at the start of the current deal. Not updated
        /// during play.
        Score : Score
    }

module Game =

    /// Creates a game.
    let create deal score =
        {
            Deal = deal
            Score = score
        }

    /// Creates a new deal.
    let createDeal rng dealer =
        Deck.shuffle rng
            |> OpenDeal.fromDeck dealer

    /// Creates a new game.
    let createNew rng dealer =
        let deal = createDeal rng dealer
        create deal Score.zero

    /// Answers the current player's information set.
    let currentInfoSet game =
        let deal = game.Deal
        let player = OpenDeal.currentPlayer deal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create
            player hand deal.ClosedDeal game.Score

    /// Adds the given bid to the given game.
    let addBid bid game =
        let deal = OpenDeal.addBid bid game.Deal
        { game with Deal = deal }

    /// Plays the given card in the given game. Score of the game
    /// is not updated, even if points are taken.
    let addPlay card game =
        let deal = OpenDeal.addPlay card game.Deal
        { game with Deal = deal }

    /// Takes the given action in the given game's current deal.
    /// Score of the game is not updated, even if points are taken.
    let addAction action game =
        let deal = OpenDeal.addAction action game.Deal
        { game with Deal = deal }

    /// Which team (if any) won the given game.
    let winningTeamOpt game =
        let maxPoint = Array.max game.Score.Points
        if maxPoint >= Setback.winThreshold then
            game.Score.Points
                |> Seq.indexed
                |> Seq.where (fun (_, pt) ->
                    pt = maxPoint)
                |> Seq.map (fst >> enum<Team>)
                |> Seq.tryExactlyOne
        else None

    /// Adds the given deal score to the given game.
    let private addDealScore dealScore (game : Game) =
        { game with Score = game.Score + dealScore }

    /// Plays one deal in a game.
    let private playDeal (playerMap : Map<_, _>) game =

        let rec loop game =

                // take action in the current deal
            let game =
                let infoSet = currentInfoSet game
                let action =
                    match Seq.tryExactlyOne infoSet.LegalActions with
                        | Some action -> action
                        | None -> playerMap[infoSet.Player].Act infoSet
                addAction action game

                // deal is over?
            if OpenDeal.isComplete game.Deal then
                match game.Deal.ClosedDeal.PlayoutOpt with
                    | Some playout when Playout.isComplete playout ->
                        let dealScore = Playout.getDealScore playout
                        addDealScore dealScore game
                    | _ -> game   // all pass auction
            else loop game

        loop game

    /// Plays the given game to completion.
    let playGame rng playerMap game =

        let rec loop game =
            assert(winningTeamOpt game |> Option.isNone)

                // play current deal to completion
            let game = playDeal playerMap game

                // stop if game is over
            match winningTeamOpt game with
                | Some team -> team
                | None ->
                        // create and play another deal
                    let deal =
                        let dealer =
                            Seat.incr 1 game.Deal.ClosedDeal.Dealer
                        createDeal rng dealer
                    loop { game with Deal = deal }

        loop game

    /// Generates an infinite sequence of games.
    let generate rng =
        Seq.initInfinite (fun iGame ->
            iGame % Seat.numSeats
                |> enum<Seat>
                |> createNew rng)

    /// Plays the given number of games.
    let playGames rng inParallel numGames playFun =
        let map =
            if inParallel then Array.Parallel.map
            else Array.map
        generate rng
            |> Seq.take numGames
            |> Seq.toArray
            |> map playFun
