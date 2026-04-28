namespace Setback.PlayModel

open PlayingCards
open Setback
open Setback.Model

(*
E+W have 10 point(s)
N+S have 9 point(s)
Game.fs.js:80 Dealer is West
Auction.fs.js:60 North bids Three
Playout.fs.js:80 North plays J♥
Playout.fs.js:80 North plays 7♦
Playout.fs.js:80 North plays 4♠
Playout.fs.js:80 North plays 4♥
Playout.fs.js:80 North plays Q♣
Playout.fs.js:80 North plays 6♦
*)

module Program =

    let run () =

        use model =
            new AdvantageModel(1200, 5, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()

        let hand =
            [ 
                "J♥"
                "7♦"
                "4♠"
                "4♥"
                "Q♣"
                "6♦"
            ]
                |> Seq.map Card.fromString
                |> set
        let deal = ClosedDeal.create Seat.West
        for ewScore = 6 to 10 do
            for nsScore = 6 to 10 do
                let score = Score.ofPoints [| ewScore; nsScore |]
                printfn $"{score}"
                let infoSet =
                    InformationSet.create Seat.North hand deal score
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
