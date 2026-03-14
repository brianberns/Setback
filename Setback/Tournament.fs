namespace Setback

open System
open PlayingCards
open Setback

module Tournament =

    /// Plays one deal.
    let private playDeal (playerMap : Map<_, _>) deal =

        let rec loop deal =
            let deal =
                let infoSet = OpenDeal.currentInfoSet deal
                let action =
                    match Seq.tryExactlyOne infoSet.LegalActions with
                        | Some action -> action
                        | None -> playerMap[infoSet.Player].Act infoSet
                OpenDeal.addAction
                    infoSet.LegalActionType action deal
            match Game.tryUpdateScore deal Score.zero with
                | Some gameScore -> gameScore
                | None -> loop deal

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
