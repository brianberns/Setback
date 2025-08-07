namespace Setback.DeepCfr.Learn

open System

open PlayingCards
open Setback

module OpenDeal =

    /// Plays the given number of deals in parallel.
    let generate (rng : Random) numDeals playFun =
        Array.init numDeals (fun iDeal ->
            let deck = Deck.shuffle rng
            let dealer =
                enum<Seat> (iDeal % Seat.numSeats)
            deck, dealer)
            |> Array.Parallel.map (fun (deck, dealer) ->
                OpenDeal.fromDeck dealer deck
                    |> OpenDeal.addBid Bid.Two
                    |> OpenDeal.addBid Bid.Pass
                    |> OpenDeal.addBid Bid.Pass
                    |> OpenDeal.addBid Bid.Pass
                    |> playFun)

    /// Gets the score of the given deal if it is complete.
    let tryGetDealScore deal =
        if ClosedDeal.isComplete deal.ClosedDeal then
            deal.ClosedDeal.PlayoutOpt
                |> Option.map Playout.getDealScore
                |> Option.defaultValue Score.zero   // all pass
                |> Some
        else None
