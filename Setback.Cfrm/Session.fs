﻿namespace Setback.Cfrm

open System
open System.ComponentModel

open PlayingCards
open Setback

/// Manages a series of games with the given players.
type Session
    (playerMap : Map<_, _>,
    rng,
    ?syncOpt : ISynchronizeInvoke) =

        // initialize events raised by this object
    let gameStartEvent = Event<_>()
    let dealStartEvent = Event<_>()
    let auctionStartEvent = Event<_>()
    let bidEvent = Event<_>()
    let auctionFinishEvent = Event<_>()
    let trickStartEvent = Event<_>()
    let playEvent = Event<_>()
    let trickFinishEvent = Event<_>()
    let dealFinishEvent = Event<_>()
    let gameFinishEvent = Event<_>()

    /// Triggers the given event safely.
    let trigger (event : Event<_>) arg =
        match syncOpt with
            | Some sync ->
                let del = Action (fun _ -> event.Trigger(arg))
                sync.Invoke(del, Array.empty) |> ignore
            | None -> event.Trigger(arg)

    /// Answers the current player's seat.
    let getSeat dealer deal =
        let iPlayer =
            deal |> AbstractOpenDeal.currentPlayerIndex
        dealer |> Seat.incr iPlayer

    /// Plays the given deal.
    let playDeal (dealer : Seat) (deal : AbstractOpenDeal) game =

        trigger dealStartEvent (dealer, deal)

            // auction
        trigger auctionStartEvent dealer.Next
        let deal =
            (deal, [1 .. Seat.numSeats])
                ||> Seq.fold (fun deal _ ->
                    let seat = getSeat dealer deal
                    let bid =
                        let player = playerMap[seat]
                        player.MakeBid game.Score deal
                    let deal = deal |> AbstractOpenDeal.addBid bid
                    trigger bidEvent (seat, bid, deal)
                    deal)
        assert(deal.ClosedDeal.Auction |> AbstractAuction.isComplete)
        trigger auctionFinishEvent ()

            // playout
        let deal =
            if deal.ClosedDeal.Auction.HighBid.Bid > Bid.Pass then
                (deal, [1 .. Setback.numCardsPerDeal])
                    ||> Seq.fold (fun deal _ ->
                        match deal.ClosedDeal.PlayoutOpt with
                            | Some playout ->

                                let numPlays = playout.CurrentTrick.NumPlays
                                let seat = getSeat dealer deal
                                if numPlays = 0 then
                                    trigger trickStartEvent seat

                                let card =
                                    let player = playerMap[seat]
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
        trigger dealFinishEvent (dealer, deal, game.Score)
        game

    /// Plays a game.
    let playGame rng dealer =

            // play deals with rotating dealer
        let rec loop dealer game =

                // play one deal
            let game =
                let deal =
                    Deck.shuffle rng
                        |> AbstractOpenDeal.fromDeck dealer
                game |> playDeal dealer deal

                // all done if game is over
            if game.Score |> BootstrapGameState.winningTeamOpt |> Option.isSome then
                dealer, game.Score

                // continue this game with next dealer
            else
                    // obtain score relative to next dealer's team
                let game =
                    let score = game.Score |> AbstractScore.shift 1
                    { game with Score = score }
                game |> loop dealer.Next

        trigger gameStartEvent ()
        let dealer, score = Game.zero |> loop dealer
        trigger gameFinishEvent (dealer, score)
        dealer

    /// Runs a session of the given number of duplicate game pairs.
    member __.Run(?nGamePairsOpt) =
        let init =
            nGamePairsOpt
                |> Option.map Seq.init
                |> Option.defaultValue Seq.initInfinite
        (Seat.South, init id)
            ||> Seq.fold (fun dealer _ ->

                    // game 1: create a fresh series of decks
                let state = Random.save rng
                let _ = playGame rng dealer

                    // game 2: repeat same series of decks with dealer shifted
                let rng = Random.restore(state)
                let dealer = playGame rng dealer.Next

                dealer.Next)
            |> ignore

    /// A game has started.
    [<CLIEvent>]
    member __.GameStartEvent = gameStartEvent.Publish

    /// A deal has started.
    [<CLIEvent>]
    member __.DealStartEvent = dealStartEvent.Publish

    /// An auction has started.
    [<CLIEvent>]
    member __.AuctionStartEvent = auctionStartEvent.Publish

    /// A bid has been made.
    [<CLIEvent>]
    member __.BidEvent = bidEvent.Publish

    /// An auction has finished.
    [<CLIEvent>]
    member __.AuctionFinishEvent = auctionFinishEvent.Publish

    /// A trick has started.
    [<CLIEvent>]
    member __.TrickStartEvent = trickStartEvent.Publish

    /// A card has been played.
    [<CLIEvent>]
    member __.PlayEvent = playEvent.Publish

    /// A trick has finished.
    [<CLIEvent>]
    member __.TrickFinishEvent = trickFinishEvent.Publish

    /// A deal has finished.
    [<CLIEvent>]
    member __.DealFinishEvent = dealFinishEvent.Publish

    /// A game has finished.
    [<CLIEvent>]
    member __.GameFinishEvent = gameFinishEvent.Publish
