namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

module Tournament =

    let private dbPlayer =
        Cfrm.DatabasePlayer.player "Setback.db"

    /// No score.
    /// Hack: Had to recreate this because module name is blocked.
    let cfrmZero =
        Array.replicate Setback.numTeams 0
            |> Cfrm.AbstractScore

    /// Plays one deal.
    let playDeal (playerMap : Map<_, Player>) deal =

        let rec auctionLoop
            (deal : OpenDeal)
            (cfrmDeal : Cfrm.AbstractOpenDeal) =
            if Auction.isComplete deal.ClosedDeal.Auction then
                deal, cfrmDeal
            else
                let bid =
                    dbPlayer.MakeBid cfrmZero cfrmDeal
                auctionLoop
                    (OpenDeal.addBid bid deal)
                    (Cfrm.AbstractOpenDeal.addBid bid cfrmDeal)

        let rec playoutLoop deal =
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let card =
                    playerMap[infoSet.Player].MakePlay infoSet
                OpenDeal.addPlay card deal
            match OpenDeal.tryGetDealScore deal with
                | Some score -> score
                | None -> playoutLoop deal

        let cfrmDeal =
            deal.UnplayedCardMap
                |> Map.map (fun _ cards ->
                    Set.toSeq cards)
                |> Cfrm.AbstractOpenDeal.fromHands
                    deal.ClosedDeal.Dealer

        let deal, cfrmDeal = auctionLoop deal cfrmDeal
        assert (ClosedDeal.isComplete deal.ClosedDeal
            = Cfrm.AbstractOpenDeal.isComplete cfrmDeal)
        if ClosedDeal.isComplete deal.ClosedDeal then
            Score.zero   // all pass
        else
            playoutLoop deal

    /// Plays the given number of deals.
    let playDeals rng numDeals playerMap =
        OpenDeal.generate rng numDeals (
            playDeal playerMap)
            |> Seq.reduce (+)

    /// Runs a tournament between two players.
    let run rng champion challenger =
        let challengerTeam = Team.EastWest
        let challengerSeats = Team.seats challengerTeam
        let playerMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let player =
                        if challengerSeats.Contains(seat) then
                            challenger
                        else champion
                    seat, player)
                |> Map
        let score =
            playDeals rng
                settings.NumEvaluationDeals
                playerMap
        let payoff =
            (ZeroSum.getPayoff score)[challengerTeam]
                / float32 settings.NumEvaluationDeals

        if settings.Verbose then
            printfn "\nTournament:"
            for (KeyValue(seat, points)) in score.ScoreMap do
                printfn $"   %-6s{string seat}: {points}"
            printfn $"   Payoff: %0.5f{payoff}"

        payoff
