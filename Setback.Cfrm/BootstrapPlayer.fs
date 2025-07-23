namespace Setback.Cfrm

open Cfrm

open Setback

/// Score-sensitive player.
module BootstrapPlayer =

    /// Score-sensitive player.
    let player baselinePlayer bootstrapPath =

        /// Score-sensitive bootstrap profile.
        let profile =
            StrategyProfile.Load(bootstrapPath)

        /// Makes a bid in the given deal.
        let makeBid score (deal : AbstractOpenDeal) =

                // get legal bids in this situation
            let auction = deal.ClosedDeal.Auction
            let legalBids =
                auction |> AbstractAuction.legalBids
            match legalBids.Length with
                | 0 -> failwith "Unexpected"
                | 1 -> legalBids[0]   // trivial case

                    // must choose between multiple legal bids
                | _ ->
                        // determine key for this situation
                    let key =
                        let hand =
                            let iPlayer =
                                deal |> AbstractOpenDeal.currentPlayerIndex
                            deal.UnplayedCards[iPlayer]
                        assert(hand.Count = Setback.numCardsPerHand)
                        BootstrapGameState.toAbbr auction score hand

                        // profile contains key?
                    profile.Best(key)
                        |> Option.map (fun iAction ->
                            legalBids[iAction])

                            // fallback
                        |> Option.defaultWith (fun () ->
                            if legalBids |> Array.contains(Bid.Three) then Bid.Three
                            else Bid.Pass)

        {
            baselinePlayer with
                MakeBid = makeBid
        }
