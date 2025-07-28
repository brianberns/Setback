namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

module Tournament =

    /// Creates and plays one deal.
    let playDeal (playerMap : Map<_, _>) deal =

        let rec loop deal score =
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let action =
                    playerMap[infoSet.Player].Act infoSet
                OpenDeal.addAction
                    infoSet.LegalActionType action deal
            match Game.tryUpdateScore deal score with
                | Some score -> score
                | None -> loop deal score

        loop deal Score.zero

    /// Plays the given number of deals.
    let playDeals rng numDeals playerMap =
        OpenDeal.generate rng numDeals (
            playDeal playerMap)
            |> Seq.reduce (+)

    /// Runs a tournament between two players.
    let run rng champion challenger =
        let challengerSeat = Seat.South
        let playerMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let player =
                        if seat = challengerSeat then challenger
                        else champion
                    seat, player)
                |> Map
        let score =
            playDeals rng
                settings.NumEvaluationDeals
                playerMap
        let payoff =
            (ZeroSum.getPayoff score)[int challengerSeat]
                / float32 settings.NumEvaluationDeals

        if settings.Verbose then
            printfn "\nTournament:"
            for (KeyValue(seat, points)) in score.ScoreMap do
                printfn $"   %-6s{string seat}: {points}"
            printfn $"   Payoff: %0.5f{payoff}"

        payoff
