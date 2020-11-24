namespace Setback.Cfrm

open System.ComponentModel

open PlayingCards
open Setback

/// A game(*) of Setback is a sequence of deals that ends when the leading
/// team's score crosses a fixed threshold.
///
/// *Once again, the terminology gets confusing: whichever team accumulates
/// the most deal points wins the game. Game points don't actually contribute
/// directly to winning a game.
type Game =
    {
        /// Player that occupies each seat.
        PlayerMap : Map<Seat, Player>

        /// Deal points taken by each team.
        Score : AbstractScore
    }
    
module Game =

    /// Creates a new game for the given players.
    let create playerMap =
        {
            PlayerMap = playerMap
            Score = AbstractScore.zero
        }

type ActionDelegate = delegate of unit -> unit

type Session
    (playerMap : Map<_, _>,
    rng,
    sync : ISynchronizeInvoke) =

        // initialize events raised by this object
    let gameStartEvent = Event<_>()
    let gameFinishEvent = Event<_>()
    let dealStartEvent = Event<_>()
    let dealFinishEvent = Event<_>()
    let bidEvent = Event<_>()
    let playEvent = Event<_>()

    let raise (event : Event<_>) arg =
        let del =
            ActionDelegate(fun _ -> event.Trigger(arg))
        sync.BeginInvoke(del, [||]) |> ignore

    let getSeat dealer deal =
        let iPlayer =
            deal |> AbstractOpenDeal.currentPlayerIndex
        dealer |> Seat.incr iPlayer

    /// Plays the given deal.
    let playDeal dealer (deal : AbstractOpenDeal) game =

        raise dealStartEvent (dealer, deal)

            // auction
        let deal =
            (deal, [1 .. Seat.numSeats])
                ||> Seq.fold (fun deal _ ->
                    let seat = getSeat dealer deal
                    let bid =
                        let player = playerMap.[seat]
                        player.MakeBid game.Score deal
                    let deal = deal |> AbstractOpenDeal.addBid bid
                    raise bidEvent (seat, bid, deal)
                    deal)
        assert(deal.ClosedDeal.Auction |> AbstractAuction.isComplete)

            // playout
        let deal =
            if deal.ClosedDeal.Auction.HighBid.Bid > Bid.Pass then
                (deal, [1 .. Setback.numCardsPerDeal])
                    ||> Seq.fold (fun deal _ ->
                        let seat = getSeat dealer deal
                        let card =
                            let player = playerMap.[seat]
                            player.MakePlay game.Score deal
                        let deal = deal |> AbstractOpenDeal.addPlay card
                        raise playEvent (seat, card, deal)
                        deal)
            else deal
                
            // update the game
        let game =
            let dealScore =
                deal |> AbstractOpenDeal.dealScore
            { game with Score = game.Score + dealScore }
        raise dealFinishEvent (deal, game.Score)
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

        raise gameStartEvent ()
        let score = game |> loop dealer
        raise gameFinishEvent score

    member __.Start() =
        Game.create playerMap
            |> playGame rng Seat.South

    [<CLIEvent>]
    member __.GameStartEvent = gameStartEvent.Publish

    [<CLIEvent>]
    member __.GameFinishEvent = gameFinishEvent.Publish

    [<CLIEvent>]
    member __.DealStartEvent = dealStartEvent.Publish

    [<CLIEvent>]
    member __.DealFinishEvent = dealFinishEvent.Publish

    [<CLIEvent>]
    member __.BidEvent = bidEvent.Publish

    [<CLIEvent>]
    member __.PlayEvent = playEvent.Publish
