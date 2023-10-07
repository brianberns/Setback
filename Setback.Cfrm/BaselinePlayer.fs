namespace Setback.Cfrm

open Cfrm

open PlayingCards
open Setback

/// A Setback player. Scores are relative to the dealer.
type Player =
    {
        /// Function that makes a bid in the given deal.
        MakeBid : AbstractScore (*relative to dealer's team*) -> AbstractOpenDeal -> Bid

        /// Function that plays a card in the given deal.
        MakePlay : AbstractScore (*relative to dealer's team*) -> AbstractOpenDeal -> Card
    }

/// Score-insensitive player.
module BaselinePlayer =

    /// Score-insenstive player.
    let player path =

        /// Strategy profile.
        let profile =
            StrategyProfile.Load(path)

        /// Makes a bid in the given deal.
        let makeBid (_ : AbstractScore) (deal : AbstractOpenDeal) =

                // get legal bids in this situation
            let auction = deal.ClosedDeal.Auction
            let legalBids =
                auction |> AbstractAuction.legalBids
            match legalBids.Length with
                | 0 -> failwith "Unexpected"
                | 1 -> legalBids[0]   // trivial case

                    // must choose between multiple legal bids
                | _ ->
                        // get legal actions (may not be 1:1 with legal bids)
                    let legalBidActions =
                        let hand = AbstractOpenDeal.currentHand deal
                        BidAction.getActions hand auction

                    let bidAction =
                        match legalBidActions.Length with
                            | 0 -> failwith "Unexpected"
                            | 1 -> legalBidActions[0]   // trivial case

                                // choose action
                            | _ ->
                                    // determine key for this situation
                                let key =
                                    let legalDealActions =
                                        legalBidActions |> Array.map DealBidAction
                                    BaselineGameState.getKey legalDealActions deal

                                    // profile contains key?
                                profile.Best(key)
                                    |> Option.map (fun iAction ->
                                        legalBidActions[iAction])

                                        // fallback
                                    |> Option.defaultWith (fun () ->
                                        if legalBids |> Array.contains Bid.Three then Bid.Three
                                        else Bid.Pass
                                        |> BidAction)

                        // convert action to bid
                    BidAction.getBid auction bidAction

        /// Plays a card in the given deal.
        let makePlay (_ : AbstractScore) (deal : AbstractOpenDeal) =

                // get legal plays in this situation
            let hand =
                AbstractOpenDeal.currentHand deal
            let handLowTrumpRankOpt =
                AbstractOpenDeal.currentLowTrumpRankOpt deal
            let playout = deal.ClosedDeal.Playout
            let legalPlays =
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Seq.toArray
            match legalPlays.Length with
                | 0 -> failwith "Unexpected"
                | 1 -> legalPlays[0]   // trivial case

                    // must choose between multiple legal plays
                | _ ->

                        // get legal actions (not usually 1:1 with legal plays)
                    let legalPlayActions =
                        PlayAction.getActions hand handLowTrumpRankOpt playout

                    let action =
                        match legalPlayActions.Length with
                            | 0 -> failwith "Unexpected"
                            | 1 -> legalPlayActions[0]   // trivial case

                                // choose action
                            | _ ->
                                    // determine key for this situation
                                let key =
                                    let legalDealActions =
                                        legalPlayActions
                                            |> Array.map DealPlayAction
                                    BaselineGameState.getKey legalDealActions deal

                                    // profile contains key?
                                profile.Best(key)
                                    |> Option.map (fun iAction ->
                                        legalPlayActions[iAction])

                                        // fallback
                                    |> Option.defaultWith (fun () ->
                                        legalPlayActions[0])

                        // convert action to card
                    PlayAction.getPlay
                        hand
                        handLowTrumpRankOpt
                        playout
                        action

        {
            MakeBid = makeBid
            MakePlay = makePlay
        }
