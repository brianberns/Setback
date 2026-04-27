namespace Setback.PlayModel

open PlayingCards
open Setback
open Setback.Model

(*
Auction.fs.js:60 East bids Two
Playout.fs.js:80 East plays Q♣
Playout.fs.js:80 East plays 4♥
Playout.fs.js:80 East plays 7♦
Playout.fs.js:80 East plays 5♦
Playout.fs.js:80 East plays T♠
Playout.fs.js:80 East plays 9♠
*)

module Program =

    let run () =

        use model =
            new AdvantageModel(1200, 5, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()

        let hand =
            [ "Q♣"; "4♥"; "7♦"; "5♦"; "T♠"; "9♠" ]
                |> Seq.map Card.fromString
                |> set
        let deal = ClosedDeal.create Seat.North
        let score = Score.zero
        let infoSet =
            InformationSet.create Seat.East hand deal score
        let strategy =
            Strategy.getFromAdvantage
                model
                [| infoSet |]
                |> Array.exactlyOne
        Array.iter2 (fun bid prob ->
            printfn $"{bid}: {prob}")
            (Array.map Action.toBid infoSet.LegalActions)
            (strategy.ToArray())

    do run ()
