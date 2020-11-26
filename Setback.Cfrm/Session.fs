namespace Setback.Cfrm

open System.ComponentModel

open PlayingCards
open Setback

/// A game of Setback is a sequence of deals that ends when the
/// leading team's score crosses a fixed threshold.
///
/// The terminology is confusing: whichever team accumulates the
/// most deal points (High, Low, Jack, Game) wins the game. Game
/// points (for face cards and tens) don't contribute directly to
/// winning a game.
type Game =
    {
        /// Deal points taken by each team (absolute index).
        Score : AbstractScore
    }
    
module Game =

    /// A new game with no score.
    let zero = { Score = AbstractScore.zero }

type ActionDelegate = delegate of unit -> unit

/// Manages a series of games with the given players.
type Session
    (playerMap : Map<_, _>,
    rng,
    sync : ISynchronizeInvoke) =

        // initialize events raised by this object
    let gameStartEvent = Event<_>()
    let gameFinishEvent = Event<_>()
    let dealStartEvent = Event<_>()
    let dealFinishEvent = Event<_>()
    let trickStartEvent = Event<_>()
    let trickFinishEvent = Event<_>()
    let bidEvent = Event<_>()
    let playEvent = Event<_>()

    /// Triggers the given event safely.
    let trigger (event : Event<_>) arg =
        let del =
            ActionDelegate(fun _ -> event.Trigger(arg))
        sync.BeginInvoke(del, [||]) |> ignore

    /// Answers the current player's seat.
    let getSeat dealer deal =
        let iPlayer =
            deal |> AbstractOpenDeal.currentPlayerIndex
        dealer |> Seat.incr iPlayer

    /// Plays the given deal.
    let playDeal dealer (deal : AbstractOpenDeal) game =

        trigger dealStartEvent (dealer, deal)

            // auction
        let deal =
            (deal, [1 .. Seat.numSeats])
                ||> Seq.fold (fun deal _ ->
                    let seat = getSeat dealer deal
                    let bid =
                        let player = playerMap.[seat]
                        player.MakeBid game.Score deal
                    let deal = deal |> AbstractOpenDeal.addBid bid
                    trigger bidEvent (seat, bid, deal)
                    deal)
        assert(deal.ClosedDeal.Auction |> AbstractAuction.isComplete)

            // playout
        let deal =
            if deal.ClosedDeal.Auction.HighBid.Bid > Bid.Pass then
                (deal, [1 .. Setback.numCardsPerDeal])
                    ||> Seq.fold (fun deal _ ->
                        match deal.ClosedDeal.PlayoutOpt with
                            | Some playout ->

                                let numPlays = playout.CurrentTrick.NumPlays
                                if numPlays = 0 then
                                    trigger trickStartEvent ()

                                let seat = getSeat dealer deal
                                let card =
                                    let player = playerMap.[seat]
                                    player.MakePlay game.Score deal
                                let deal = deal |> AbstractOpenDeal.addPlay card
                                trigger playEvent (seat, card, deal)

                                if numPlays = Seat.numSeats - 1 then
                                    trigger trickFinishEvent ()

                                deal
                            | None -> failwith "Unexpected")
            else deal
                
            // update the game
        let game =
            let dealScore =
                deal |> AbstractOpenDeal.dealScore
            { game with Score = game.Score + dealScore }
        trigger dealFinishEvent (deal, game.Score)
        game

    /// Plays the given game.
    let playGame rng dealer game =

            // play deals with rotating dealer
        let rec loop dealer game =

                // play one deal
            let game =
                let deal =
                    Deck.shuffle rng
                        |> AbstractOpenDeal.fromDeck dealer
                game |> playDeal dealer deal

                // continue this game?
            if game.Score |> BootstrapGameState.winningTeamOpt |> Option.isSome then
                game.Score
            else
                game |> loop dealer.Next

        trigger gameStartEvent ()
        let score = game |> loop dealer
        trigger gameFinishEvent score

    member __.Start() =
        Game.zero |> playGame rng Seat.South

    [<CLIEvent>]
    member __.GameStartEvent = gameStartEvent.Publish

    [<CLIEvent>]
    member __.GameFinishEvent = gameFinishEvent.Publish

    [<CLIEvent>]
    member __.DealStartEvent = dealStartEvent.Publish

    [<CLIEvent>]
    member __.DealFinishEvent = dealFinishEvent.Publish

    [<CLIEvent>]
    member __.TrickStartEvent = trickStartEvent.Publish

    [<CLIEvent>]
    member __.TrickFinishEvent = trickFinishEvent.Publish

    [<CLIEvent>]
    member __.BidEvent = bidEvent.Publish

    [<CLIEvent>]
    member __.PlayEvent = playEvent.Publish
