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

    /// Gets the score of the given deal if it is complete.
    let rec tryGetDealScore deal =

        let rec loop deal =
            if ClosedDeal.isComplete deal.ClosedDeal then
                deal
            else
                match deal.ClosedDeal.PlayoutOpt with
                    | Some playout ->
                        let player = ClosedDeal.currentPlayer deal.ClosedDeal
                        let hand = deal.UnplayedCardMap[player]
                        let card = Trickster.makePlay player hand deal.ClosedDeal.Auction playout
                        OpenDeal.addPlay card deal
                    | None -> deal

        if ClosedDeal.isComplete deal.ClosedDeal then
            deal.ClosedDeal.PlayoutOpt
                |> Option.map Playout.getDealScore
                |> Option.defaultValue Score.zero   // all pass
                |> Some
        else
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout ->
                    if playout.TrumpOpt.IsNone then None
                    else loop deal |> tryGetDealScore
                | None -> None
