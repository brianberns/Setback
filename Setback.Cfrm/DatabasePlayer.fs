namespace Setback.Cfrm

open System
open System.Data
open System.Data.SQLite

open Setback

/// Load and run player from database.
module DatabasePlayer =

    /// Database player.
    let player databasePath =

        /// Database connection.
        let conn = 
            let connStr = $"DataSource={databasePath};Version=3;"
            let conn = new SQLiteConnection(connStr)
            conn.Open()
            conn

        /// SQL command.
        let selectActionIndexCmd =
            new SQLiteCommand(
                "select ActionIndex \
                from Strategy \
                where Key = @Key",
                conn
            )

        /// SQL command parameter.
        let keyParam =
            selectActionIndexCmd.Parameters.Add("Key", DbType.String)

        /// Finds the action index for the given key, if any.
        let getActionIndex key =
            keyParam.Value <- key
            let value = selectActionIndexCmd.ExecuteScalar()
            if DBNull.Value.Equals(value) then None
            else value :?> int64 |> int |> Some

        /// Makes a bid in the given deal.
        let makeBid score deal =

                // get legal bids in this situation
            let auction = deal.ClosedDeal.Auction
            let legalBids =
                auction |> AbstractAuction.legalBids
            match legalBids.Length with
                | 0 -> failwith "Unexpected"
                | 1 -> legalBids.[0]   // trivial case

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
                    key
                        |> getActionIndex
                        |> Option.map (fun iAction ->
                            legalBids.[iAction])

                            // fallback
                        |> Option.defaultWith (fun () ->
                            if legalBids |> Array.contains(Bid.Three) then Bid.Three
                            else Bid.Pass)

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
                | 1 -> legalPlays.[0]   // trivial case

                    // must choose between multiple legal plays
                | _ ->

                        // get legal actions (not usually 1:1 with legal plays)
                    let legalPlayActions =
                        PlayAction.getActions hand handLowTrumpRankOpt playout

                    let action =
                        match legalPlayActions.Length with
                            | 0 -> failwith "Unexpected"
                            | 1 -> legalPlayActions.[0]   // trivial case

                                // choose action
                            | _ ->
                                    // determine key for this situation
                                let key =
                                    let legalDealActions =
                                        legalPlayActions
                                            |> Array.map DealPlayAction
                                    BaselineGameState.getKey legalDealActions deal

                                    // profile contains key?
                                key
                                    |> getActionIndex
                                    |> Option.map (fun iAction ->
                                        legalPlayActions.[iAction])

                                        // fallback
                                    |> Option.defaultWith (fun () ->
                                        legalPlayActions.[0])

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
