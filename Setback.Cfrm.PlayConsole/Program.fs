open System

open Setback
open Setback.Cfrm

let player =

    let makeBid _ (openDeal : AbstractOpenDeal) =
        openDeal.ClosedDeal.Auction
            |> AbstractAuction.legalBids
            |> Seq.head

    let makePlay _ (openDeal : AbstractOpenDeal) =
        match openDeal.ClosedDeal.PlayoutOpt with
            | Some playout ->
                let iPlayer =
                    playout |> AbstractPlayout.currentPlayerIndex
                let hand =
                    openDeal.UnplayedCards.[iPlayer]
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Seq.head
            | None -> failwith "Unexpected"

    {
        MakeBid = makeBid
        MakePlay = makePlay
    }

let session =
    let playerMap =
        Map [
            Seat.West, player
            Seat.North, player
            Seat.East, player
            Seat.South, player
        ]
    let rng = Random(0)
    Session(playerMap, rng)

module AbstractScore =

    let toAbbr (score : AbstractScore) =
        $"{score.[0]}-{score.[1]}"

let onGameStart () =
    printfn "Game start"
    session.StartDeal(Seat.South)

let onGameFinish (gameScore : AbstractScore, seriesScore : AbstractScore) =
    printfn "Game finish"
    printfn $"   Final game score:     {AbstractScore.toAbbr gameScore}"
    printfn $"   Updated series score: {AbstractScore.toAbbr seriesScore}"
    session.StartGame()

let onDealStart (openDeal : AbstractOpenDeal) =
    let hand = openDeal.UnplayedCards.[0] |> Seq.sortDescending
    printfn "Deal start"
    for card in hand do
        printfn $"   {card}"
    session.FinishDeal()

let onDealFinish (openDeal : AbstractOpenDeal, gameScore : AbstractScore) =
    printfn "Deal finish"
    printfn $"   Final deal score:   {openDeal |> AbstractOpenDeal.dealScore |> AbstractScore.toAbbr}"
    printfn $"   Updated game score: {AbstractScore.toAbbr gameScore}"
    session.FinishGame()

[<EntryPoint>]
let main argv =
    session.GameStartEvent.Add onGameStart
    session.GameFinishEvent.Add onGameFinish
    session.DealStartEvent.Add onDealStart
    session.DealFinishEvent.Add onDealFinish
    session.StartGame()
    0
