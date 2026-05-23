namespace Setback.PlayModel

open System
open System.Text

open PlayingCards
open Setback
open Setback.Model

module Program =

    let private getModel () =
        let model =
            new AdvantageModel(1250, 6, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()
        model

    let run1 () =

        use model = getModel ()

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

    let run2 () =

        use model = getModel ()

        let hand =
            [ 
                "A♥"
                "J♥"
                "5♠"
                "T♦"
                "2♠"
                "2♣"
            ]
                |> Seq.map Card.fromString
                |> set
        let deal =
            ClosedDeal.create Seat.West
        let infoSet =
            InformationSet.create Seat.North hand deal Score.zero
        let strategy =
            Strategy.getFromAdvantage
                model
                [| infoSet |]
                |> Array.exactlyOne
        Array.iter2 (fun bid prob ->
            printfn $"   {bid}: {prob}")
            (Array.map Action.toBid infoSet.LegalActions)
            (strategy.ToArray())

    let run3 () =

        use model = getModel ()

        let hand =
            [ 
                "K♥"
                "5♥"
                "3♥"
                "4♣"
                "7♠"
                "5♦"
            ]
                |> Seq.map Card.fromString
                |> set
        let deal =
            ClosedDeal.create Seat.West
                |> ClosedDeal.addBid Bid.Two
                |> ClosedDeal.addBid Bid.Pass
                |> ClosedDeal.addBid Bid.Pass
                |> ClosedDeal.addBid Bid.Pass
        let infoSet =
            InformationSet.create Seat.North hand deal Score.zero
        let strategy =
            Strategy.getFromAdvantage
                model
                [| infoSet |]
                |> Array.exactlyOne
        Array.iter2 (fun card prob ->
            printfn $"   {card}: {prob}")
            (Array.map Action.toPlay infoSet.LegalActions)
            (strategy.ToArray())

    do
        Console.OutputEncoding <- Encoding.UTF8
        run3 ()
