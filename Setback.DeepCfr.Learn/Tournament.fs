namespace Setback.DeepCfr.Learn

open Setback

module Tournament =

    let challengerTeam = Team.EastWest
    let challengerSeats = Team.seats challengerTeam
    let championTeam = Team.NorthSouth
    let championSeats = Team.seats challengerTeam

    /// Creates and plays one deal.
    let playDeal (playerMap : Map<_, _>) deal =

        let rec loop deal =
            let infoSet = OpenDeal.currentInfoSet deal
            let player = infoSet.Player
            let action = playerMap[player].Act infoSet
            let deal' = OpenDeal.addAction action deal
            match OpenDeal.tryGetDealScore deal' with
                | Some score ->
                    if challengerSeats.Contains(player) then
                        let payoff = (ZeroSum.getPayoff score)[challengerTeam]
                        let otherAction = playerMap[Seat.next player].Act infoSet
                        if otherAction <> action then
                            let otherDeal = OpenDeal.addAction otherAction deal
                            let otherScore = (OpenDeal.tryGetDealScore otherDeal).Value
                            let otherPayoff = (ZeroSum.getPayoff otherScore)[challengerTeam]
                            match action, otherAction with
                                | MakePlay play, MakePlay otherPlay ->
                                    // lock challengerSeats (fun () ->
                                    //    printfn $"{PlayingCards.Hand.toString infoSet.Hand}, {play}, {otherPlay}, {payoff - otherPayoff}")
                                    ()
                                | _ -> failwith "Unexpected"
                    score
                | None -> loop deal'

        loop deal

    /// Plays the given number of deals.
    let playDeals rng numDeals playerMap =
        OpenDeal.generate rng numDeals (
            playDeal playerMap)
            |> Seq.reduce (+)

    /// Runs a tournament between two players.
    let run rng champion challenger =
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
            for (KeyValue(team, points)) in score.ScoreMap do
                printfn $"   %-6s{Team.toString team}: {points}"
            printfn $"   Payoff: %0.5f{payoff}"

        payoff
