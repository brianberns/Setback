namespace Setback.Cfrm

open PlayingCards
open Setback

open Microsoft.Data.Sqlite

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

        /// Strategy database connection.
        let conn =
            let conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly")
            conn.Open()
            conn

        /// Tries to lookup the best action for the given deal.
        let tryGetActionIndex openDeal =
            let dealActions = openDeal |> AbstractOpenDeal.getActions
            use cmd =
                conn.CreateCommand(
                    CommandText =
                        "select Action from Strategy where Key = $Key")
            let key = BaselineGameState.getKey dealActions openDeal
            cmd.Parameters.AddWithValue("$Key", key)
                |> ignore

            let actionObj = cmd.ExecuteScalar()
            if isNull actionObj then None
            else
                let actionIdx = actionObj :?> int64 |> int
                assert(actionIdx >= 0
                    && actionIdx < dealActions.Length)
                Some actionIdx

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
                                tryGetActionIndex deal
                                    |> Option.map (fun iAction ->
                                        legalBidActions[iAction])
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
                                tryGetActionIndex deal
                                    |> Option.map (fun iAction ->
                                        legalPlayActions[iAction])
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
