namespace Setback.DeepCfr.PlayGui

open System
open System.ComponentModel

open PlayingCards
open Setback

module Score =

    /// Which team (if any) won the game with the given score.
    let winningTeamOpt score =
        let maxScore = Seq.max score.ScoreMap.Values
        if maxScore >= Setback.winThreshold then
            score.ScoreMap
                |> Map.toSeq
                |> Seq.where (fun (_, score) ->
                    score = maxScore)
                |> Seq.map fst
                |> Seq.tryExactlyOne
        else None

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
    let getSeat deal =
        ClosedDeal.currentPlayer deal.ClosedDeal

    /// Plays the given deal.
    let playDeal (dealer : Seat) (deal : OpenDeal) gameScore =

        trigger dealStartEvent (dealer, deal)

            // auction
        trigger auctionStartEvent dealer.Next
        let deal =
            deal
                |> OpenDeal.addBid Bid.Two
                |> OpenDeal.addBid Bid.Pass
                |> OpenDeal.addBid Bid.Pass
                |> OpenDeal.addBid Bid.Pass
        assert(deal.ClosedDeal.Auction |> Auction.isComplete)
        trigger auctionFinishEvent ()

            // playout
        let deal =
            if deal.ClosedDeal.Auction.HighBid > Bid.Pass then
                (deal, [1 .. Setback.numCardsPerDeal])
                    ||> Seq.fold (fun deal _ ->
                        let numPlays =
                            match deal.ClosedDeal.PlayoutOpt with
                                | Some playout ->
                                    match playout.CurrentTrickOpt with
                                        | Some trick -> trick.Cards.Length
                                        | None -> failwith "No trick"
                                | None -> failwith "No playout"
                        let seat = getSeat deal
                        if numPlays = 0 then
                            trigger trickStartEvent seat

                        let infoSet = OpenDeal.currentInfoSet deal
                        let player = playerMap[infoSet.Player]
                        let card =
                            match player.Act infoSet with
                                | MakeBid _ -> failwith "Unexpected"
                                | MakePlay card -> card
                        let deal = deal |> OpenDeal.addPlay card
                        trigger playEvent (seat, card, deal)

                        if numPlays = Seat.numSeats - 1 then
                            trigger trickFinishEvent ()

                        deal)
            else deal
                
            // update the game score
        let gameScore =
            (*
            let dealScore =
                deal |> OpenDeal.dealScore
            { Score = game.Score + dealScore }
            *)
            gameScore
        trigger dealFinishEvent (dealer, deal, gameScore)
        gameScore

    /// Plays a game.
    let playGame rng dealer =

            // play deals with rotating dealer
        let rec loop dealer gameScore =

                // play one deal
            let gameScore =
                let deal =
                    Deck.shuffle rng
                        |> OpenDeal.fromDeck dealer
                gameScore |> playDeal dealer deal

                // all done if game is over
            if gameScore |> Score.winningTeamOpt |> Option.isSome then
                dealer, gameScore

                // continue this game with next dealer
            else
                gameScore |> loop dealer.Next

        trigger gameStartEvent ()
        let dealer, score = Score.zero |> loop dealer
        trigger gameFinishEvent (dealer, score)
        dealer

    member _.PlayDeal(dealer, deal, game) =
        playDeal dealer deal game

    /// Runs a session of the given number of duplicate game pairs.
    member _.Run(?nGamePairsOpt) =
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
    member _.GameStartEvent = gameStartEvent.Publish

    /// A deal has started.
    [<CLIEvent>]
    member _.DealStartEvent = dealStartEvent.Publish

    /// An auction has started.
    [<CLIEvent>]
    member _.AuctionStartEvent = auctionStartEvent.Publish

    /// A bid has been made.
    [<CLIEvent>]
    member _.BidEvent = bidEvent.Publish

    /// An auction has finished.
    [<CLIEvent>]
    member _.AuctionFinishEvent = auctionFinishEvent.Publish

    /// A trick has started.
    [<CLIEvent>]
    member _.TrickStartEvent = trickStartEvent.Publish

    /// A card has been played.
    [<CLIEvent>]
    member _.PlayEvent = playEvent.Publish

    /// A trick has finished.
    [<CLIEvent>]
    member _.TrickFinishEvent = trickFinishEvent.Publish

    /// A deal has finished.
    [<CLIEvent>]
    member _.DealFinishEvent = dealFinishEvent.Publish

    /// A game has finished.
    [<CLIEvent>]
    member _.GameFinishEvent = gameFinishEvent.Publish
