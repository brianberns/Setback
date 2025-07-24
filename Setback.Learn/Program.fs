namespace Setback.Learn

open PlayingCards
open Setback

module Program =

    let teamNS = { Seats = [ Seat.North; Seat.South ]; Number = 0 }
    let teamEW = { Seats = [ Seat.East ; Seat.West  ]; Number = 1 }
    let teams = [| teamNS; teamEW |]

    let getHand names =
        names
            |> Seq.map (fun name -> Card.fromString name)
            |> Seq.toArray

    let getHands pairs =
        pairs
            |> Seq.sortBy fst
            |> Seq.map (fun (_, names) -> getHand names)
            |> Seq.toArray

    let test () =

        let hands =
            [
                (Seat.West,  ["JS"; "3H"; "KC"; "8C"; "3C"; "9D"])
                (Seat.North, ["9S"; "4S"; "TC"; "9C"; "8D"; "6D"])
                (Seat.East,  ["TS"; "8S"; "KH"; "6H"; "2H"; "QC"])
                (Seat.South, ["KS"; "5S"; "9H"; "8H"; "3D"; "2D"])
            ] |> getHands
        let deal = OpenDeal.fromHands teams Seat.North hands

        let deal = deal.AddBid Bid.Three
        let deal = deal.AddBid Bid.Pass
        let deal = deal.AddBid Bid.Pass
        let deal = deal.AddBid Bid.Pass

        let deal =
            (deal, [
                "KH"; "9H"; "3H"; "6D";
                "8S"; "KS"; "JS"; "4S";
                "8H"; "3C"; "8D";
            ]) ||> Seq.fold (fun deal name ->
                deal.AddPlay(Card.fromString name))
        printfn "%A" deal

    System.Console.OutputEncoding <- System.Text.Encoding.Unicode
    test ()
