namespace Setback

open System
open PlayingCards
open Setback

/// Model Setback as a zero-sum game.
module ZeroSum =

    /// Gets the payoff for the given raw score from each team's
    /// point of view.
    let getPayoff score =
        let points = score.Points
        assert(points.Length = Team.numTeams)
        let sum = Seq.sum points
        [|
            for pt in points do
                let otherAvg =
                    float32 (sum - pt)
                        / float32 (Seat.numSeats - 1)
                otherAvg - float32 pt
        |]

module Tournament =

    /// Plays one deal.
    let private playDeal (playerMap : Map<_, _>) deal =

        let rec loop deal =

                // take an action
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let action =
                    match Seq.tryExactlyOne infoSet.LegalActions with
                        | Some action -> action
                        | None -> playerMap[infoSet.Player].Act infoSet
                OpenDeal.addAction action deal

                // loop again?
            if ClosedDeal.isComplete deal.ClosedDeal then
                match deal.ClosedDeal.PlayoutOpt with
                    | Some playout when Playout.isComplete playout ->
                        Playout.getDealScore playout
                    | _ -> Score.zero   // all pass auction
            else loop deal

        loop deal

    /// Plays the given number of deals.
    let private playDeals rng inParallel numDeals playerMap =
        OpenDeal.playDeals rng inParallel numDeals (
            playDeal playerMap)
            |> Seq.reduce (+)

    /// Runs a 2v2 tournament between two players.
    let run rngSeed inParallel numDeals champion challenger =

        let runWith numDeals (challengerSeats : Set<_>) =
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
                let rng = Random(rngSeed)
                playDeals rng inParallel numDeals playerMap
            let payoffs = ZeroSum.getPayoff score
            challengerSeats
                |> Seq.sumBy (fun seat -> payoffs[int seat])

            // duplicate deals, so each deal runs twice
        assert(numDeals % 2 = 0)
        let halfDeals = numDeals / 2

            // champion and challenger are represented equally
        assert(Seat.numSeats % 2 = 0)
        let nSeats = Seat.numSeats / 2

        let sumA = runWith halfDeals (set [ Seat.East; Seat.West ])
        let sumB = runWith halfDeals (set [ Seat.North; Seat.South ])
        (sumA + sumB) / float32 (nSeats * numDeals)
