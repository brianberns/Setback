namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

module Tournament =

    /// Creates and plays one deal.
    let playDeal (playerMap : Map<_, _>) deal =

        let rec loop deal =
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let action =
                    playerMap[infoSet.Player].Act infoSet
                OpenDeal.addAction action deal
            match OpenDeal.tryGetDealScore deal with
                | Some score -> score
                | None -> loop deal

        loop deal

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
            Seat.allSeats
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
