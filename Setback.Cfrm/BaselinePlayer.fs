namespace Setback.Cfrm

open Cfrm

open PlayingCards
open Setback

/// A Setback player.
type Player<'bidExtra, 'playExtra> =
    {
        /// Function that makes a bid in the given deal.
        MakeBid : AbstractScore -> AbstractOpenDeal -> (Bid * Option<'bidExtra>)

        /// Function that plays a card in the given deal.
        MakePlay : AbstractScore -> AbstractOpenDeal -> (Card * Option<'playExtra>)
    }

/// Score-insensitive player.
module BaselinePlayer =

    /// Extra information associated with taking an action in a particular
    /// situation (i.e. "info set").
    type Extra =
        {
            /// Situation key.
            Key : string

            /// Probability of taking each legal action in this situation.
            Probabilities : (string (*action name*) * float)[]
        }

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
                | 1 -> legalBids.[0], None   // trivial case

                    // must choose between multiple legal bids
                | _ ->
                        // get legal actions (may not be 1:1 with legal bids)
                    let legalBidActions =
                        let hand = AbstractOpenDeal.currentHand deal
                        BidAction.getActions hand auction

                    let bidAction, key, strategy =
                        match legalBidActions.Length with
                            | 0 -> failwith "Unexpected"
                            | 1 -> legalBidActions.[0], "Forced", [| 1.0 |]   // trivial case

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
                                        legalBidActions.[iAction], key, profile.Map.[key])

                                        // fallback
                                    |> Option.defaultWith (fun () ->
                                        let legalBidSet = set legalBids
                                        let bid =
                                            if legalBidSet.Contains(Bid.Three) then Bid.Three
                                            else Bid.Pass
                                        let zeros = Array.replicate legalBidActions.Length 0.0
                                        BidAction bid, key, zeros)

                        // convert action to bid
                    let bid = BidAction.getBid auction bidAction

                        // assemble extra information
                    let extraOpt =
                        let names =
                            legalBidActions
                                |> Seq.map (fun (BidAction bid) ->
                                    bid.ToString())
                                |> Seq.toArray
                        Some {
                            Key = key
                            Probabilities = (Array.zip names strategy)
                        }

                    bid, extraOpt

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
                | 1 -> legalPlays.[0], None   // trivial case

                    // must choose between multiple legal plays
                | _ ->

                        // get legal actions (not usually 1:1 with legal plays)
                    let legalPlayActions =
                        PlayAction.getActions hand handLowTrumpRankOpt playout

                    let action, key, strategy =
                        match legalPlayActions.Length with
                            | 0 -> failwith "Unexpected"
                            | 1 -> legalPlayActions.[0], "Forced", [| 1.0 |]   // trivial case

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
                                        legalPlayActions.[iAction], key, profile.Map.[key])

                                        // fallback
                                    |> Option.defaultWith (fun () ->
                                        let zeros = Array.replicate legalPlayActions.Length 0.0
                                        legalPlayActions.[0], key, zeros)

                        // convert action to card
                    let card =
                        PlayAction.getPlay
                            hand
                            handLowTrumpRankOpt
                            playout
                            action

                        // assemble extra information
                    let extraOpt =
                        let names =
                            legalPlayActions
                                |> Seq.map (function
                                    | Lead action -> action.ToString()
                                    | Follow action -> action.ToString())
                                |> Seq.toArray
                        Some {
                            Key = key
                            Probabilities = (Array.zip names strategy)
                        }

                    card, extraOpt

        {
            MakeBid = makeBid
            MakePlay = makePlay
        }
