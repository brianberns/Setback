namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module Playout =

    /// Answers legal plays in the given hand and deal.
    let private getLegalPlays hand closedDeal =
        match closedDeal.PlayoutOpt with
            | Some playout ->
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Set.ofSeq
            | _ -> failwith "Unexpected"

    /// Playout context.
    type private Context =
        {
            /// Current dealer's seat.
            Dealer : Seat

            /// Current deal.
            Deal : AbstractOpenDeal

            /// Animation of playing a card.
            AnimCardPlay : CardView -> Animation

            /// Animation of winning a trick.
            AnimTrickFinish : Seat -> Animation

            /// Animation of establishing trump.
            AnimEstablishTrump : Seat -> Suit -> Animation
        }

    /// Plays the given card on the current trick, and returns the
    /// seat of the resulting trick winner, if any.
    let private getTrickWinnerOpt context card =
        assert(context.Deal.ClosedDeal.PlayoutOpt.IsSome)
        option {
                // get trump suit, if any
            let! playout = context.Deal.ClosedDeal.PlayoutOpt
            let! trump = playout.TrumpOpt

                // play card on current trick
            let trick =
                playout.CurrentTrick
                    |> AbstractTrick.addPlay trump card

                // if this card completes the trick, determine winner
            if trick |> AbstractTrick.isComplete then
                return context.Dealer
                    |> Seat.incr (
                        AbstractTrick.highPlayerIndex trick)
        }

    /// Has trump just been established?
    let private tryTrumpJustEstablished deal =
        option {
            let! playout = deal.ClosedDeal.PlayoutOpt
            if playout.CurrentTrick.NumPlays = 1
                && playout.History = AbstractPlayoutHistory.empty then
                    assert(playout.TrumpOpt.IsSome)
                    return! playout.TrumpOpt
        }

    /// Plays the given card in the given deal and then continues
    /// the rest of the deal.
    let private playCard context cardView card =
        assert(cardView |> CardView.card = card)
        promise {

                // write to log
            let seat =
                AbstractOpenDeal.getCurrentSeat
                    context.Dealer
                    context.Deal
            console.log($"{Seat.toString seat} plays {card}")

                // add the card to the deal
            let deal =
                context.Deal
                    |> AbstractOpenDeal.addPlay card

                // animate if setting trump
            match tryTrumpJustEstablished deal with
                | Some trump ->
                    do! context.AnimEstablishTrump seat trump
                        |> Animation.run
                | None -> ()

                // play the card
            do! context.AnimCardPlay cardView
                |> Animation.run

                // trick is complete?
            let dealComplete = deal |> AbstractOpenDeal.isComplete
            match getTrickWinnerOpt context card with
                | Some winner ->

                        // track game points won
                    DealView.displayStatus context.Dealer deal

                        // animate
                    let animate () =
                        context.AnimTrickFinish winner
                            |> Animation.run
                    if winner.IsUser && not dealComplete then
                        animate () |> ignore   // don't force user to wait for animation to finish
                    else
                        do! animate ()

                | None -> ()

                // cleanup at end of deal
            if dealComplete then
                do! AuctionView.removeAnim ()
                    |> Animation.run

            return deal
        }

    /// Allows user to play a card.
    let private playUser (handView : HandView) context =

            // determine all legal plays
        let legalPlays =
            let hand =
                AbstractOpenDeal.currentHand context.Deal
            getLegalPlays hand context.Deal.ClosedDeal
        assert(not legalPlays.IsEmpty)

            // enable user to select one of the corresponding card views
        Promise.create(fun resolve _reject ->
            for cardView in handView do
                let card = cardView |> CardView.card
                if legalPlays.Contains(card) then
                    cardView.addClass("active")
                    cardView.click(fun () ->

                            // prevent further clicks
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
            let! card =
                WebPlayer.makePlay AbstractScore.zero context.Deal

                // create view of the selected card
            let! cardView =
                let isTrump =
                    option {
                        let! playout = context.Deal.ClosedDeal.PlayoutOpt
                        let! trump = playout.TrumpOpt
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
    let run dealer deal (playoutMap : Map<_, _>) =
        assert(
            deal.ClosedDeal.Auction
                |> AbstractAuction.isComplete)

        /// Plays a single card and then loops recursively.
        let rec loop deal =
            async {
                    // prepare current player
                let seat =
                    AbstractOpenDeal.getCurrentSeat dealer deal
                let (handView : HandView),
                    animCardPlay,
                    animTrickFinish,
                    animEstablishTrump =
                        playoutMap[seat]
                let player =
                    if seat.IsUser then
                        playUser handView >> Async.AwaitPromise
                    else
                        playAuto

                    // invoke player
                let! deal' =
                    player {
                        Dealer = dealer
                        Deal = deal
                        AnimCardPlay = animCardPlay
                        AnimTrickFinish = animTrickFinish
                        AnimEstablishTrump = animEstablishTrump
                    }

                    // recurse until auction is complete
                let isComplete =
                    deal' |> AbstractOpenDeal.isComplete
                if isComplete then
                    return deal'
                else
                    return! loop deal'
            }

        loop deal
