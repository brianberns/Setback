namespace Setback.Cfrm

open System

open PlayingCards
open Setback

/// A series of games for a group of players.
type GameSeries =
    {
        /// Automated players in this series.
        PlayerMap : Map<Seat, Player>

        /// Count of games won by each team in this series.
        Score : AbstractScore

        /// Random number generator.
        Rng : Random
    }

module GameSeries =

    /// Starts a series of games.
    let start playerMap rng =
        {
            PlayerMap = playerMap
            Score = AbstractScore.zero
            Rng = rng
        }

/// A game within a series of games.
type Game =
    {
        /// Containing series.
        Series : GameSeries

        /// Score of the game.
        Score : AbstractScore
    }

module Game =

    /// Starts a game in the given series.
    let start series =
        {
            Series = series
            Score = AbstractScore.zero
        }

    /// Finishes a won game.
    let finish game =

            // determine winning team
        let iTeam =
            match BootstrapGameState.winningTeamScoreOpt game.Score with
                | Some iTeam -> iTeam
                | None -> failwith "No winning team"

            // update series score
        let incr = AbstractScore.forTeam iTeam 1
        {
            game.Series with
                Score = game.Series.Score + incr
        }

/// A deal within a game.
type GameDeal =
    {
        /// Containing game.
        Game : Game

        /// Dealer for this deal.
        Dealer : Seat

        /// Underlying deal.
        OpenDeal : AbstractOpenDeal
    }

module GameDeal =

    /// Starts a deal with the given dealer.
    let start game dealer =
        {
            Game = game
            Dealer = dealer
            OpenDeal =
                Deck.shuffle game.Series.Rng
                    |> AbstractOpenDeal.fromDeck dealer
        }

    /// Finishes a deal.
    let finish gameDeal =
        assert(gameDeal.OpenDeal |> AbstractOpenDeal.isComplete)

            // update score of game
        let dealScore =
            gameDeal.OpenDeal |> AbstractOpenDeal.dealScore
        {
            gameDeal.Game with
                Score = gameDeal.Game.Score + dealScore
        }

type Session(playerMap, rng) =

        // initialize events raised by this object
    let gameStartEvent = new Event<_>()
    let gameFinishEvent = new Event<_>()
    let dealStartEvent = new Event<_>()
    let dealFinishEvent = new Event<_>()
    let bidEvent = new Event<_>()
    (*
    let turnStartEvent = new Event<_>()
    let turnFinishEvent = new Event<_>()
    *)
    (*
    let userBidEvent = new Event<_>()
    let trickBeginEvent = new Event<_>()
    let trickEndEvent = new Event<_>()
    let playEvent = new Event<_>()
    let userPlayEvent = new Event<_>()
    *)

    /// Current game series.
    let mutable series =
        GameSeries.start playerMap rng

    /// Current game.
    let mutable gameOpt =
        Option<Game>.None

    /// Current game deal
    let mutable gameDealOpt =
        Option<GameDeal>.None

    /// Starts a new game.
    member __.StartGame() =
        assert(gameOpt.IsNone)
        assert(gameDealOpt.IsNone)

            // start game
        let game = Game.start series

            // update state
        gameOpt <- Some game

            // trigger event
        gameStartEvent.Trigger()

    /// Starts a new deal.
    member __.StartDeal(dealer) =
        assert(gameDealOpt.IsNone)

        match gameOpt with
            | Some game ->

                    // start deal
                let gameDeal = GameDeal.start game dealer

                    // update state
                gameDealOpt <- Some gameDeal

                    // trigger event
                dealStartEvent.Trigger(gameDeal.OpenDeal)

            | None -> failwith "No active game"

    member __.DoTurn() =
        assert(gameOpt.IsSome)
        match gameDealOpt, gameOpt with
            | Some gameDeal, Some game ->

                    // get player's bid
                let auction = gameDeal.OpenDeal.ClosedDeal.Auction
                assert(auction |> AbstractAuction.isComplete |> not)
                let seat =
                    let iPlayer =
                        auction |> AbstractAuction.currentBidderIndex
                    gameDeal.Dealer |> Seat.incr iPlayer
                let bid =
                    playerMap.[seat].MakeBid game.Score gameDeal.OpenDeal

                    // update state
                let openDeal =
                    let closedDeal =
                        let auction = auction |> AbstractAuction.addBid bid
                        { gameDeal.OpenDeal.ClosedDeal with Auction = auction }
                    { gameDeal.OpenDeal with ClosedDeal = closedDeal }
                gameDealOpt <- Some { gameDeal with OpenDeal = openDeal }

                    // trigger event
                bidEvent.Trigger(seat, bid, openDeal)

            | None, _ -> failwith "No active deal"
            | _, None -> failwith "No active game"

    (*
    member __.StartTurn() =
        assert(gameOpt.IsSome)
        match gameDealOpt with
            | Some gameDeal ->
                turnStartEvent.Trigger(gameDeal.OpenDeal)
            | None -> failwith "No active deal"

    member __.FinishTurn() =
        assert(gameOpt.IsSome)
        match gameDealOpt with
            | Some gameDeal ->
                turnFinishEvent.Trigger(gameDeal.OpenDeal)
            | None -> failwith "No active deal"
    *)

    member __.FinishDeal() =
        assert(gameOpt.IsSome)
        match gameDealOpt with
            | Some gameDeal ->

                    // finish deal
                let game = GameDeal.finish gameDeal

                    // update state
                gameOpt <- Some game
                gameDealOpt <- None

                    // trigger event
                dealFinishEvent.Trigger(gameDeal.OpenDeal, game.Score)

            | None -> failwith "No active deal"

    member __.FinishGame() =
        match gameOpt with
            | Some game ->

                    // finish game and update state
                series <- Game.finish game
                gameOpt <- None

                    // trigger event
                gameFinishEvent.Trigger(game.Score, series.Score)

            | None -> failwith "No active game"

    [<CLIEvent>]
    member __.GameStartEvent = gameStartEvent.Publish

    [<CLIEvent>]
    member __.GameFinishEvent = gameFinishEvent.Publish

    [<CLIEvent>]
    member __.DealStartEvent = dealStartEvent.Publish

    [<CLIEvent>]
    member __.DealFinishEvent = dealFinishEvent.Publish

    (*
    [<CLIEvent>]
    member __.TurnStartEvent = turnStartEvent.Publish

    [<CLIEvent>]
    member __.TurnFinishEvent = turnFinishEvent.Publish
    *)

    [<CLIEvent>]
    member __.BidEvent = bidEvent.Publish
