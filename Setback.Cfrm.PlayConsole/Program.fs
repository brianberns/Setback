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
    session.StartDeal(Seat.South)

let onDealStart (_ : AbstractOpenDeal) =
    printfn "Deal start"
    session.DoTurn()

let onBid (seat : Seat, bid : Bid, openDeal : AbstractOpenDeal) =
    printfn $"{seat} bids {bid}"
    if openDeal |> AbstractOpenDeal.isComplete then
        assert(openDeal.ClosedDeal.Auction.HighBid.Bid = Bid.Pass)
        session.FinishDeal()
    else
        session.DoTurn()

let onPlay (seat : Seat, card : Card, openDeal : AbstractOpenDeal) =
    printfn $"{seat} plays {card}"
    if openDeal |> AbstractOpenDeal.isComplete then
        session.FinishDeal()
    else
        session.DoTurn()

(*
let onTurnStart (openDeal : AbstractOpenDeal) =
    let iPlayer =
        openDeal |> AbstractOpenDeal.currentPlayerIndex
    printfn $"Turn start for player {iPlayer}"
    let hand = openDeal.UnplayedCards.[0] |> Seq.sortDescending
    for card in hand do
        printfn $"   {card}"
    session.FinishTurn()

let onTurnFinish (openDeal : AbstractOpenDeal) =
    let iPlayer =
        openDeal |> AbstractOpenDeal.currentPlayerIndex
    printfn $"Turn finish for player {iPlayer}"
*)

let onDealFinish (openDeal : AbstractOpenDeal, gameScore : AbstractScore) =
    printfn "Deal finish"
    printfn $"   Final deal score:   {openDeal |> AbstractOpenDeal.dealScore |> AbstractScore.toAbbr}"
    printfn $"   Updated game score: {AbstractScore.toAbbr gameScore}"
    session.FinishGame()

let onGameFinish (gameScore : AbstractScore, seriesScore : AbstractScore) =
    printfn "Game finish"
    printfn $"   Final game score:     {AbstractScore.toAbbr gameScore}"
    printfn $"   Updated series score: {AbstractScore.toAbbr seriesScore}"
    session.StartGame()

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
    try
        session.StartGame()
    with ex ->
        printfn $"{ex.Message}"
        printfn $"{ex.StackTrace.[0..400]}"
    0
