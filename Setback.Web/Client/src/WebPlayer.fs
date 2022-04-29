namespace Setback.Web.Client

module Remoting =

    open Fable.Remoting.Client
    open Setback.Web

    let api =
        Remoting.createApi()
            |> Remoting.buildProxy<ISetbackApi>

module WebPlayer =

    open Setback
    open Setback.Cfrm

    /// Makes a bid in the given deal.
    let makeBid score deal =

            // get legal bids in this situation
        let auction = deal.ClosedDeal.Auction
        let legalBids =
            auction |> AbstractAuction.legalBids
        async {
            match legalBids.Length with
                | 0 -> return failwith "Unexpected"   // why does FABLE require "return" here?
                | 1 -> return legalBids.[0]           // trivial case

                    // must choose between multiple legal bids
                | _ ->
                        // determine key for this situation
                    let key =
                        let hand =
                            let iPlayer =
                                deal |> AbstractOpenDeal.currentPlayerIndex
                            deal.UnplayedCards.[iPlayer]
                        assert(hand.Count = Setback.numCardsPerHand)
                        BootstrapGameState.toAbbr auction score hand

                        // profile contains key?
                    match! Remoting.api.GetActionIndex(key) with
                        | Some iAction ->
                            return legalBids.[iAction]
                        | None ->
                            return
                                if legalBids |> Array.contains(Bid.Three) then Bid.Three
                                else Bid.Pass
        }

    /// Chooses a play action.
    let private chooseAction (legalPlayActions : _[]) deal =
        async {
            match legalPlayActions.Length with
                | 0 -> return failwith "Unexpected"
                | 1 -> return legalPlayActions.[0]   // trivial case

                    // choose action
                | _ ->
                        // determine key for this situation
                    let key =
                        let legalDealActions =
                            legalPlayActions
                                |> Array.map DealPlayAction
                        BaselineGameState.getKey legalDealActions deal

                        // profile contains key?
                    match! Remoting.api.GetActionIndex(key) with
                        | Some iAction ->
                            return legalPlayActions.[iAction]
                        | None ->
                            return legalPlayActions.[0]
        }

    /// Plays a card in the given deal.
    let makePlay (_ : AbstractScore) (deal : AbstractOpenDeal) =

            // get legal plays in this situation
        let hand =
            AbstractOpenDeal.currentHand deal
        let handLowTrumpRankOpt =
            AbstractOpenDeal.currentLowTrumpRankOpt deal
        let playout, legalPlays =
            match deal.ClosedDeal.PlayoutOpt with
                | Some playout ->
                    let legalPlays =
                        playout
                            |> AbstractPlayout.legalPlays hand
                            |> Seq.toArray
                    playout, legalPlays
                | _ -> failwith "Unexpected"
        async {
            match legalPlays.Length with
                | 0 -> return failwith "Unexpected"   // why does FABLE require "return" here?
                | 1 -> return legalPlays.[0]          // trivial case

                    // must choose between multiple legal plays
                | _ ->

                        // get legal actions (not usually 1:1 with legal plays)
                    let legalPlayActions =
                        PlayAction.getActions hand handLowTrumpRankOpt playout

                        // choose best action
                    let! action = chooseAction legalPlayActions deal

                        // convert action to card
                    return PlayAction.getPlay
                        hand
                        handLowTrumpRankOpt
                        playout
                        action
        }
