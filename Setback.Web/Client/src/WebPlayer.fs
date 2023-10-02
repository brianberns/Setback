namespace Setback.Web.Client

module Remoting =

    open Fable.Remoting.Client
    open Browser.Dom
    open Setback.Web

    /// Prefix routes with /Setback.
    let routeBuilder typeName methodName = 
        sprintf "/Setback/%s/%s" typeName methodName

    /// Server API.
    let api =
        Remoting.createApi()
            |> Remoting.withRouteBuilder routeBuilder
            |> Remoting.buildProxy<ISetbackApi>

/// Plays Setback by calling a remote server.
module WebPlayer =

    open Setback
    open Setback.Cfrm

    /// Makes a bid in the given deal.
    let makeBid score deal =

            // get legal bids in this situation
        let auction = deal.ClosedDeal.Auction
        let legalBids =
            auction |> AbstractAuction.legalBids

        match legalBids.Length with
            | 0 -> failwith "Unexpected"
            | 1 -> async.Return(legalBids[0])          // trivial case

                // must choose between multiple legal bids
            | _ ->
                async {
                        // determine key for this situation
                    let key =
                        let hand =
                            let iPlayer =
                                deal |> AbstractOpenDeal.currentPlayerIndex
                            deal.UnplayedCards[iPlayer]
                        assert(hand.Count = Setback.numCardsPerHand)
                        BootstrapGameState.toAbbr auction score hand             // score-sensitive bidding

                        // profile contains key?
                    match! Remoting.api.GetActionIndex(key) with
                        | Some iAction ->
                            return legalBids[iAction]
                        | None ->
                            return
                                if legalBids |> Array.contains(Bid.Three) then   // assumption: unusual hand is probably strong
                                    Bid.Three
                                else Bid.Pass
                }

    /// Chooses a play action.
    let private chooseAction (legalPlayActions : _[]) deal =
        match legalPlayActions.Length with
            | 0 -> failwith "Unexpected"
            | 1 -> async.Return(legalPlayActions[0])   // trivial case

                // choose action
            | _ ->
                async {
                        // determine key for this situation
                    let key =
                        let legalDealActions =
                            legalPlayActions
                                |> Array.map DealPlayAction
                        BaselineGameState.getKey legalDealActions deal

                        // profile contains key?
                    match! Remoting.api.GetActionIndex(key) with
                        | Some iAction ->
                            return legalPlayActions[iAction]
                        | None ->
                            return legalPlayActions[0]
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

        match legalPlays.Length with
            | 0 -> failwith "Unexpected"
            | 1 -> async.Return(legalPlays[0])          // trivial case

                // must choose between multiple legal plays
            | _ ->
                async {
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
