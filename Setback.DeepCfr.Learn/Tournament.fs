namespace Setback.DeepCfr.Learn

open Setback

module Tournament =

    /// Plays one deal pair.
    let playDealPair (playerMap : Map<_, _>) deal =

        let rec loop deal =
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let action =
                    playerMap[infoSet.Player].Act infoSet
                OpenDeal.addAction action deal
            match OpenDeal.tryGetDealScore deal with
                | Some score -> score
                | None -> loop deal

        let hands = deal.UnplayedCardMap.Values
        Seat.allSeats
            |> Seq.map (fun seat ->
                let handMap =
                    let seats = Seat.cycle seat
                    Seq.zip seats hands |> Map
                { deal with UnplayedCardMap = handMap })
            |> Seq.map loop
            |> Seq.reduce (+)

    /// Plays the given number of deal pairs.
    let playDeals rng numDealPairs playerMap =
        OpenDeal.generate rng numDealPairs (
            playDealPair playerMap)
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
                settings.NumEvaluationDealGroups
                playerMap
        let payoff =
            (ZeroSum.getPayoff score)[challengerTeam]
                / float32 (settings.NumEvaluationDealGroups * Seat.numSeats)

        if settings.Verbose then
            printfn "\nTournament:"
            for (KeyValue(team, points)) in score.ScoreMap do
                printfn $"   %-6s{Team.toString team}: {points}"
            printfn $"   Payoff: %0.5f{payoff}"

        payoff
