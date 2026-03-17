namespace Setback

open System
open PlayingCards

/// A game of Setback.
type Game =
    {
        /// RNG used to create new deals.
        Random : Random

        /// Current deal, or winning team (if the game is over).
        Current : Choice<OpenDeal, Team>

        /// Game score at the start of the current deal. Not updated
        /// during play.
        Score : Score
    }

    /// Current deal, assuming game is not over.
    member game.Deal =
        match game.Current with
            | Choice1Of2 deal -> deal
            | Choice2Of2 _ -> failwith "Unexpected"

module Game =

    /// Creates a new deal.
    let private createDeal rng dealer =
        Deck.shuffle rng
            |> OpenDeal.fromDeck dealer

    /// Creates a new game.
    let create dealer =
        let rng = Random()
        let deal = createDeal rng dealer
        {
            Random = rng
            Current = Choice1Of2 deal
            Score = Score.zero
        }

    /// Answers the current player's information set.
    let currentInfoSet (game : Game) =
        let deal = game.Deal
        let player = OpenDeal.currentPlayer deal
        let hand = deal.UnplayedCardMap[player]
        InformationSet.create
            player hand deal.ClosedDeal game.Score

    /// Which team (if any) won the game with the given score.
    let private tryGetWinningTeam score =
        let maxPoint = Array.max score.Points
        if maxPoint >= Setback.winThreshold then
            score.Points
                |> Seq.indexed
                |> Seq.where (fun (_, pt) ->
                    pt = maxPoint)
                |> Seq.map (fst >> enum<Team>)
                |> Seq.tryExactlyOne
        else None

    /// Takes the given action in the given game's current deal.
    /// At the end of each deal, the score is updated, and, if
    /// the game is not over, a new deal is created.
    let addAction action (game : Game) =

            // update the current deal
        let deal = OpenDeal.addAction action game.Deal

            // end of deal bookkeeping
        if OpenDeal.isComplete deal then

                // update score
            let game =
                match game.Deal.ClosedDeal.PlayoutOpt with
                    | Some playout ->
                        assert(Playout.isComplete playout)
                        let dealScore = Playout.getDealScore playout
                        { game with Score = game.Score + dealScore }
                    | _ ->
                        assert(Auction.isComplete game.Deal.ClosedDeal.Auction)
                        assert(game.Deal.ClosedDeal.Auction.HighBid = Bid.Pass)
                        game   // all pass auction

                // game is over?
            match tryGetWinningTeam game.Score with
                | Some team ->
                    { game with Current = Choice2Of2 team }
                | None ->
                    let dealer =
                        Seat.incr 1 game.Deal.ClosedDeal.Dealer
                    let deal = createDeal game.Random dealer
                    { game with Current = Choice1Of2 deal }

        else game

    /// Plays the given game to completion.
    let playGame (playerMap : Map<_, _>) game =

        let rec loop game =

            let infoSet = currentInfoSet game
            let action =
                match Seq.tryExactlyOne infoSet.LegalActions with
                    | Some action -> action
                    | None -> playerMap[infoSet.Player].Act infoSet
            let game = addAction action game
            match game.Current with
                | Choice1Of2 _ -> loop game
                | Choice2Of2 team -> team   // game is over

        loop game

    /// Generates an infinite sequence of games.
    let generate () =
        Seq.initInfinite (fun iGame ->
            iGame % Seat.numSeats
                |> enum<Seat>
                |> create)

    /// Plays the given number of games.
    let playGames inParallel numGames playFun =
        let map =
            if inParallel then Array.Parallel.map
            else Array.map
        generate ()
            |> Seq.take numGames
            |> Seq.toArray
            |> map playFun
