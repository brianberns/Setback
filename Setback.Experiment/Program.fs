namespace Setback.PlayModel

open PlayingCards
open Setback
open Setback.Model

(*
Dealer is East
Auction.fs.js:60 South bids Three
Auction.fs.js:60 West bids Four
Auction.fs.js:60 North bids Pass
Auction.fs.js:60 East bids Four
Playout.fs.js:80 East plays J♦
Playout.fs.js:80 East plays 9♦
Playout.fs.js:80 East plays 3♣
Playout.fs.js:80 East plays 3♠
Playout.fs.js:80 East plays 8♣
Playout.fs.js:80 East plays 9♣
*)

module Program =

    let run () =

        use model =
            new AdvantageModel(1250, 6, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()

        let hand =
            [ 
                "J♦"
                "9♦"
                "3♣"
                "3♠"
                "8♣"
                "9♣"
            ]
                |> Seq.map Card.fromString
                |> set
        let deal =
            ClosedDeal.create Seat.East
                |> ClosedDeal.addBid Bid.Three
                |> ClosedDeal.addBid Bid.Four
                |> ClosedDeal.addBid Bid.Pass
        let infoSet =
            InformationSet.create Seat.East hand deal Score.zero
        let strategy =
            Strategy.getFromAdvantage
                model
                [| infoSet |]
                |> Array.exactlyOne
        Array.iter2 (fun bid prob ->
            printfn $"   {bid}: {prob}")
            (Array.map Action.toBid infoSet.LegalActions)
            (strategy.ToArray())

    do run ()
