namespace Setback.Cfrm

open System

open PlayingCards
open Setback

/// Session interface.
type ISession =

    /// A new game has started within a series.
    abstract member RaiseGameStart :
        unit -> unit

    /// A game has finished.
    abstract member RaiseGameFinish :
        AbstractScore (*final game score*)
            -> AbstractScore (*resulting series score*)
            -> unit

    /// A new deal has started within a game.
    abstract member RaiseDealStart :
        AbstractOpenDeal -> unit

    /// A deal has finished.
    abstract member RaiseDealFinish :
        AbstractOpenDeal (*final deal state*)
            -> AbstractScore (*resulting game score*)
            -> unit

/// A series of games for a group of players.
type GameSeries =
    {
        /// Interactive session.
        Session : ISession

        /// Automated players in this series.
        PlayerMap : Map<Seat, Player>

        /// Count of games won by each team in this series.
        Score : AbstractScore

        /// Random number generator.
        Rng : Random
    }

module GameSeries =

    /// Starts a series of games.
    let start session playerMap rng =
        {
            Session = session
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
                | None -> 0 // failwith "No winning team"

            // update series score
        let series =
            let incr = AbstractScore.forTeam iTeam 1
            {
                game.Series with
                    Score = game.Series.Score + incr
            }

            // raise event
        series.Session.RaiseGameFinish game.Score series.Score
        series

/// A deal within a game.
type GameDeal =
    {
        /// Containing game.
        Game : Game

        /// Deck used for this deal.
        Deck : Deck

        /// Dealer for this deal.
        Dealer : Seat

        /// Underlying deal.
        OpenDeal : AbstractOpenDeal
    }

module GameDeal =

    /// Starts a deal with the given dealer.
    let start game dealer =
        let deal =
            let deck = Deck.shuffle game.Series.Rng
            {
                Game = game
                Deck = deck
                Dealer = dealer
                OpenDeal = AbstractOpenDeal.fromDeck dealer deck
            }
        game.Series.Session.RaiseDealStart deal.OpenDeal
        deal

    /// Finishes a deal.
    let finish deal =
        assert(deal.OpenDeal |> AbstractOpenDeal.isComplete)

            // update score of game
        let game =
            let dealScore =
                deal.OpenDeal |> AbstractOpenDeal.dealScore
            {
                deal.Game with
                    Score = deal.Game.Score + dealScore
            }

            // raise event
        game.Series.Session.RaiseDealFinish deal.OpenDeal game.Score

type Session(playerMap, rng) as session =

        // initialize events raised by this object
    let gameStartEvent = new Event<_>()
    let gameFinishEvent = new Event<_>()
    let dealStartEvent = new Event<_>()
    let dealFinishEvent = new Event<_>()
    (*
    let turnBeginEvent = new Event<_>()
    let turnEndEvent = new Event<_>()
    let bidEvent = new Event<_>()
    let userBidEvent = new Event<_>()
    let trickBeginEvent = new Event<_>()
    let trickEndEvent = new Event<_>()
    let playEvent = new Event<_>()
    let userPlayEvent = new Event<_>()
    *)

    let mutable series =
        GameSeries.start session playerMap rng

    let mutable gameOpt =
        Option<Game>.None

    member __.RaiseGameStart () =
        gameStartEvent.Trigger()

    member __.RaiseGameFinish gameScore seriesScore =
        gameFinishEvent.Trigger(gameScore, seriesScore)

    member __.RaiseDealStart openDeal =
        dealStartEvent.Trigger(openDeal)

    member __.RaiseDealFinish openDeal gameScore =
        dealFinishEvent.Trigger(openDeal, gameScore)

    interface ISession with

        member session.RaiseGameStart () =
            session.RaiseGameStart()

        member session.RaiseGameFinish gameScore seriesScore =
            session.RaiseGameFinish gameScore seriesScore

        member __.RaiseDealStart openDeal =
            session.RaiseDealStart openDeal

        member __.RaiseDealFinish openDeal gameScore =
            session.RaiseDealFinish openDeal gameScore

    [<CLIEvent>]
    member __.GameStartEvent = gameStartEvent.Publish

    [<CLIEvent>]
    member __.GameFinishEvent = gameFinishEvent.Publish

    [<CLIEvent>]
    member __.DealStartEvent = dealStartEvent.Publish

    [<CLIEvent>]
    member __.DealFinishEvent = dealFinishEvent.Publish

    member __.Start() =
        gameOpt <- Game.start series |> Some
        session.RaiseGameStart ()

    member __.FinishGame() =
        series <-
            match gameOpt with
                | Some game -> game |> Game.finish
                | None -> failwith "No game started"
