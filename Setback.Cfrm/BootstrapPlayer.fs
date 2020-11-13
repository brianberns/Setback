namespace Setback.Cfrm

open Cfrm

open Setback

/// Score-sensitive player.
module BootstrapPlayer =

    /// Score-sensitive player.
    let player baselinePlayer bootstrapPath =

        /// Bootstrap profile.
        let profile =
            StrategyProfile.Load(bootstrapPath)

        /// Makes a bid in the given deal.
        let makeBid (score : AbstractScore) (deal : AbstractOpenDeal) =

                // get legal bids in this situation
            let auction = deal.ClosedDeal.Auction
            let legalBids =
                auction |> AbstractAuction.legalBids
            match legalBids.Length with
                | 0 -> failwith "Unexpected"
                | 1 -> legalBids.[0], None   // trivial case

                    // must choose between multiple legal bids
                | _ ->
                    let bid, key, strategy =

                            // determine key for this situation
                        let key =
                            let hand =
                                let iPlayer =
                                    deal |> AbstractOpenDeal.currentPlayerIndex
                                deal.UnplayedCards.[iPlayer]
                            assert(hand.Count = Setback.numCardsPerHand)
                            BootstrapGameState.toAbbr auction score hand

                            // profile contains key?
                        profile.Best(key)
                            |> Option.map (fun iAction ->
                                legalBids.[iAction], key, profile.Map.[key])

                                // fallback
                            |> Option.defaultWith (fun () ->
                                let legalBidSet = set legalBids
                                let bid =
                                    if legalBidSet.Contains(Bid.Three) then Bid.Three
                                    else Bid.Pass
                                let zeros = Array.replicate legalBids.Length 0.0
                                bid, key, zeros)

                        // assemble extra information
                    let extraOpt =
                        (*
                        let names =
                            legalBids
                                |> Seq.map (fun bid ->
                                    bid.ToString())
                                |> Seq.toArray
                        Some {
                            BaselinePlayer.Extra.Key = key
                            BaselinePlayer.Extra.Probabilities = (Array.zip names strategy)
                        }
                        *)
                        None

                    bid, extraOpt

        {
            baselinePlayer with
                MakeBid = makeBid
        }
