namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback

module Playout =

    /// Playout context.
    type private Context =
        {
            /// Current game.
            Game : Game

            /// Animation of playing a card.
            AnimCardPlay : CardView -> Animation

            /// Animation of winning a trick.
            AnimTrickFinish : Seat -> Animation

            /// Animation of establishing trump.
            AnimEstablishTrump : Seat -> Suit -> Animation
        }

    /// Plays the given card on the current trick to determine the
    /// seat of the resulting trick winner, if any.
    let private getTrickWinnerOpt context card =
        option {
                // get trump suit
            assert(context.Game.Deal.ClosedDeal.PlayoutOpt.IsSome)
            let! playout = context.Game.Deal.ClosedDeal.PlayoutOpt
            let! trump = playout.TrumpOpt   // if trump not yet established, this card can't complete the trick

                // play card on current trick
            assert(playout.CurrentTrickOpt.IsSome)
            let! trick = playout.CurrentTrickOpt
            let trick = Trick.addPlay trump card trick   // playout is not affected

                // if this card completes the trick, determine winner
            assert(Trick.highPlayerOpt trick |> Option.isSome)
            if Trick.isComplete trick then
                return! Trick.highPlayerOpt trick
        }

    /// Plays the given card in the given game and then continues
    /// the rest of the game.
    let private playCard context cardView card =
        assert(cardView |> CardView.card = card)
        promise {

                // write to log
            let deal = context.Game.Deal
            let seat = OpenDeal.currentPlayer deal
            console.log($"{Seat.toString seat} plays {card}")

                // animate if setting trump
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout when Playout.numCardsPlayed playout = 0 ->
                    do! context.AnimEstablishTrump seat card.Suit
                        |> Animation.run
                | Some _ -> ()
                | None -> failwith "Unexpected"

                // play the card on the deal
            let deal = deal |> OpenDeal.addPlay card
            do! context.AnimCardPlay cardView
                |> Animation.run

                // trick is complete?
            match getTrickWinnerOpt context card with
                | Some winner ->

                        // track game points won
                    DealView.displayStatus deal

                        // animate
                    let animate () =
                        context.AnimTrickFinish winner
                            |> Animation.run
                    let dealComplete = deal |> OpenDeal.isComplete
                    if winner.IsUser && not dealComplete then
                        animate () |> ignore   // don't force user to wait for animation to finish
                    else
                        do! animate ()

                | None -> ()

            return context.Game
                |> Game.addAction Random.Shared (Choice2Of2 card)   // can cause a new deal to start
        }

    /// Allows user to play a card.
    let private playUser chooser (handView : HandView) context =

            // determine all legal plays
        let infoSet = Game.currentInfoSet context.Game
        let legalPlays =
            infoSet.LegalActions
                |> Seq.map Action.toPlay
                |> set
        assert(not legalPlays.IsEmpty)

            // enable user to select one of the corresponding card views
        Promise.create(fun resolve _reject ->
            chooser |> PlayChooser.display
            for cardView in handView do
                let card = cardView |> CardView.card
                if legalPlays.Contains(card) then
                    cardView.addClass("active")
                    cardView.click(fun () ->

                            // prevent further clicks
                        chooser |> PlayChooser.hide
                        for cardView in handView do
                            cardView.removeClass("active")
                            cardView.removeClass("inactive")
                            cardView.off("click")

                            // play the selected card
                        promise {
                            let! deal = playCard context cardView card
                            resolve deal
                        } |> ignore)
                else
                    cardView.addClass("inactive"))

    /// Automatically plays a card.
    let private playAuto context =
        async {
                // determine card to play
            let! action = WebPlayer.takeAction context.Game
            let card = Action.toPlay action

                // create view of the selected card
            let! cardView =
                let isTrump =
                    option {
                        let! trump = context.Game.Deal.ClosedDeal.TrumpOpt
                        return card.Suit = trump
                    } |> Option.defaultValue true
                card
                    |> CardView.ofCard isTrump
                    |> Async.AwaitPromise

                // play the card
            return! playCard context cardView card
                |> Async.AwaitPromise
        }

    /// Runs the given deal's playout
    let run (persState : PersistentState) chooser (playoutMap : Map<_, _>) =
        assert(
            persState.Game.Deal.ClosedDeal.Auction
                |> Auction.isComplete)

        /// Plays a single card and then loops recursively.
        let rec loop (persState : PersistentState) =
            async {
                let deal = persState.Game.Deal
                let isComplete = OpenDeal.isComplete deal
                if isComplete then

                        // cleanup at end of deal
                    do! AuctionView.removeAnim ()
                        |> Animation.run
                        |> Async.AwaitPromise

                    return persState
                else
                        // prepare current player
                    let seat = OpenDeal.currentPlayer deal
                    let (handView : HandView),
                        animCardPlay,
                        animTrickFinish,
                        animEstablishTrump =
                            playoutMap[seat]
                    let player =
                        if seat.IsUser then
                            playUser chooser handView >> Async.AwaitPromise
                        else playAuto

                        // invoke player
                    let! game =
                        player {
                            Game = persState.Game
                            AnimCardPlay = animCardPlay
                            AnimTrickFinish = animTrickFinish
                            AnimEstablishTrump = animEstablishTrump
                        }

                        // recurse until playout is complete
                    let persState' =
                        { persState with Game = game }
                    let save =   // save at trick boundary
                                option {
                            let! playout = game.Deal.ClosedDeal.PlayoutOpt
                                    let! trick = playout.CurrentTrickOpt
                                    return trick.Cards.Length % Seat.numSeats = 0
                                } |> Option.defaultValue true
                    if save then
                        PersistentState.save persState'
                    return! loop persState'
            }

        loop persState
