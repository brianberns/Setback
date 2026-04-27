namespace Setback.PlayModel

open PlayingCards
open Setback
open Setback.Model

(*
{"VersionNum": 1, "GamesWon": {"Points": [1, 0]}, "Game": {"Deal": {"ClosedDeal": {"Auction": {"Dealer": 3, "Bids": [ 0, 0, 2], "HighBidderOpt": 0, "HighBid": 2}, "PlayoutOpt": null}, "UnplayedCardMap": [[0, [{"Rank": 5, "Suit": 3}, {"Rank": 6, "Suit": 3}, {"Rank": 11, "Suit": 0}, {"Rank": 12, "Suit": 0}, {"Rank": 12, "Suit": 3}, {"Rank": 14, "Suit": 1}]], [1, [{"Rank": 5, "Suit": 2}, {"Rank": 8, "Suit": 1}, {"Rank": 8, "Suit": 2}, {"Rank": 10, "Suit": 1}, {"Rank": 10, "Suit": 2}, {"Rank": 14, "Suit": 0}]], [2, [{"Rank": 2, "Suit": 3}, {"Rank": 6, "Suit": 0}, {"Rank": 7, "Suit": 3}, {"Rank": 8, "Suit": 0}, {"Rank": 12, "Suit": 1}, {"Rank": 13, "Suit": 2}]], [3, [{"Rank": 6, "Suit": 2}, {"Rank": 7, "Suit": 1}, {"Rank": 10, "Suit": 0}, {"Rank": 11, "Suit": 2}, {"Rank": 13, "Suit": 0}, {"Rank": 13, "Suit": 3}]]]}, "Score": {"Points": [10, 7]}}}
*)

module Program =

    let run () =

        use model =
            new AdvantageModel(1152, 4, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()

        let hand =
            [ "KS"; "JH"; "6H"; "KC"; "TC"; "7D" ]
                |> Seq.map Card.fromString
                |> set
        let deal =
            ClosedDeal.create Seat.South
                |> ClosedDeal.addBid Bid.Two
                |> ClosedDeal.addBid Bid.Pass
                |> ClosedDeal.addBid Bid.Pass
        for ewScore = 0 to 10 do
            for nsScore = 0 to 10 do
                let score = Score.ofPoints [| ewScore; nsScore |]
                printfn $"Score: {score}"
                let infoSet =
                    InformationSet.create Seat.South hand deal score
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
