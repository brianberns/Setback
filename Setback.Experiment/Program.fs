namespace Setback.PlayModel

open System
open System.IO

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

    let runModel (model : AdvantageModel) path =

        model.load(path : string) |> ignore
        model.eval()

        let cards =
            [|
                for suit in [ Suit.Spades; Suit.Diamonds; Suit.Hearts ] do
                    for rankNum = 2 to 10 do
                        Card(enum<Rank> rankNum, suit)
            |]
        [
            for _ = 1 to 1000 do
                let hand =
                    set [
                        Card.fromString "QC"
                        yield! Array.randomShuffle cards |> Array.take 5
                    ]
                // printfn $"{Hand.toString hand}"
                let deal = ClosedDeal.create Seat.North
                let infoSet =
                    InformationSet.create Seat.East hand deal Score.zero
                let strategy =
                    Strategy.getFromAdvantage
                        model
                        [| infoSet |]
                        |> Array.exactlyOne
                yield! Array.zip
                    (Array.map Action.toBid infoSet.LegalActions)
                    (strategy.ToArray())
        ]
            |> Seq.groupBy fst
            |> Seq.map (fun (bid, group) ->
                let avg =
                    Seq.map snd group
                        |> Seq.average
                bid, avg)
            |> Seq.iter (fun (bid, avg) ->
                printfn $"   {bid}: {avg}")

    let run () =
        use model =
            new AdvantageModel(1200, 5, 0.0, TorchSharp.torch.CPU)
        let dir = DirectoryInfo(@"C:\Users\brian\iCloudDrive\Setback\iter006\Training large")
        for file in dir.GetFiles("*.pt") do
            printfn $"{file.Name}"
            runModel model file.FullName

    do
        Console.OutputEncoding <- Text.Encoding.UTF8
        run ()
