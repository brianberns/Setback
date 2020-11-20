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

let onGameStart () =
    printfn "Game start"
    session.FinishGame()

let onGameFinish (gameScore, seriesScore) =
    printfn $"Game finish: {gameScore}, {seriesScore}"

let onDealStart openDeal =
    printfn $"Deal start: {openDeal}"

let onDealFinish (openDeal, gameScore) =
    printfn $"Deal finish: {openDeal}, {gameScore}"

[<EntryPoint>]
let main argv =
    session.GameStartEvent.Add onGameStart
    session.GameFinishEvent.Add onGameFinish
    session.DealStartEvent.Add onDealStart
    session.DealFinishEvent.Add onDealFinish
    session.Start()
    0
