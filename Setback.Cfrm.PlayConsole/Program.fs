open System

open PlayingCards
open Setback
open Setback.Cfrm

let rng = Random(0)

let chooseRandom (items : _[]) =
    let idx = rng.Next(items.Length)
    items.[idx]

let player =

    let makeBid _ (openDeal : AbstractOpenDeal) =
        openDeal.ClosedDeal.Auction
            |> AbstractAuction.legalBids
            |> chooseRandom

    let makePlay _ (openDeal : AbstractOpenDeal) =
        match openDeal.ClosedDeal.PlayoutOpt with
            | Some playout ->
                let iPlayer =
                    playout |> AbstractPlayout.currentPlayerIndex
                let hand =
                    openDeal.UnplayedCards.[iPlayer]
                playout
                    |> AbstractPlayout.legalPlays hand
                    |> Seq.toArray
                    |> chooseRandom
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
    Session(playerMap, rng)

module AbstractScore =

    let toAbbr (score : AbstractScore) =
        $"{score.[0]}-{score.[1]}"

let onGameStart () =
    printfn "Game start"

let onDealStart (_ : AbstractOpenDeal) =
    printfn "Deal start"

let onBid (seat : Seat, bid : Bid, openDeal : AbstractOpenDeal) =
    printfn $"{seat} bids {bid}"

let onPlay (seat : Seat, card : Card, openDeal : AbstractOpenDeal) =
    printfn $"{seat} plays {card}"

let onDealFinish (openDeal : AbstractOpenDeal, gameScore : AbstractScore) =
    printfn "Deal finish"
    printfn $"   Final deal score:   {openDeal |> AbstractOpenDeal.dealScore |> AbstractScore.toAbbr}"
    printfn $"   Updated game score: {AbstractScore.toAbbr gameScore}"

let onGameFinish (gameScore : AbstractScore (*, seriesScore : AbstractScore*)) =
    printfn "Game finish"
    printfn $"   Final game score:     {AbstractScore.toAbbr gameScore}"
    // printfn $"   Updated series score: {AbstractScore.toAbbr seriesScore}"

let init () =

    session.GameStartEvent.Add onGameStart
    session.DealStartEvent.Add onDealStart
    (*
    session.TurnStartEvent.Add onTurnStart
    session.TurnFinishEvent.Add onTurnFinish
    *)
    session.BidEvent.Add onBid
    session.PlayEvent.Add onPlay
    session.DealFinishEvent.Add onDealFinish
    session.GameFinishEvent.Add onGameFinish

[<EntryPoint>]
let main argv =
    init ()
    let rng = Random(0)
    try
        let iWinningTeam = session.Start(rng)
        printfn $"Winning team: {iWinningTeam}"
    with ex ->
        printfn $"{ex.Message}"
        printfn $"{ex.StackTrace.[0..400]}"
    0
