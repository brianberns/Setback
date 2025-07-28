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
                    |> playFun)
