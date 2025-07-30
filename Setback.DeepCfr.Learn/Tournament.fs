namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

/// Interface for a Setback player.
type Player =
    {
        /// Chooses an action in the given information set.
        MakePlay : (InformationSet * Cfrm.AbstractOpenDeal) -> Card
    }

module Tournament =

    /// Creates a Setback player using the given model.
    let createPlayer model =

        let rng = System.Random()   // each player has its own RNG

        let makePlay infoSet =
            let strategy =
                DeepCfr.Model.Strategy.getFromAdvantage model [|infoSet|]
                    |> Array.exactlyOne
            Setback.DeepCfr.Model.Vector.sample rng strategy
                |> Array.get infoSet.LegalPlays

        { MakePlay = fst >> makePlay }

    /// No score.
    /// Hack: Had to recreate this because module name is blocked.
    let cfrmZero =
        Array.replicate Setback.numTeams 0
            |> Cfrm.AbstractScore

    let private dbPlayer =
        Cfrm.DatabasePlayer.player "Setback.db"

    let champion =
        let makePlay cfrmDeal =
            dbPlayer.MakePlay cfrmZero cfrmDeal
        {
            MakePlay = (snd >> makePlay)
        }

    /// Plays one deal.
    let playDeal (playerMap : Map<_, _>) deal =

        let rec auctionLoop deal cfrmDeal =
            if Auction.isComplete deal.ClosedDeal.Auction then
                deal, cfrmDeal
            else
                let bid =
                    dbPlayer.MakeBid cfrmZero cfrmDeal
                auctionLoop
                    (OpenDeal.addBid bid deal)
                    (Cfrm.AbstractOpenDeal.addBid bid cfrmDeal)

        let rec playoutLoop deal cfrmDeal =
            let deal, cfrmDeal =
                let infoSet = OpenDeal.currentInfoSet deal
                let card =
                    playerMap[infoSet.Player].MakePlay (infoSet, cfrmDeal)
                OpenDeal.addPlay card deal,
                Cfrm.AbstractOpenDeal.addPlay card cfrmDeal
            match OpenDeal.tryGetDealScore deal with
                | Some score -> score
                | None -> playoutLoop deal cfrmDeal

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
            playoutLoop deal cfrmDeal

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
