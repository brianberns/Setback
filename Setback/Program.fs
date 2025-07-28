namespace Setback

open PlayingCards

module Program =

    let getHand names =
        names
            |> Seq.map Card.fromString
            |> set

    let getHands pairs =
        pairs
            |> Seq.map (fun (seat, names) ->
                seat, getHand names)
            |> Map

    let onePlay () =

        let hands =
            [
                Seat.West,  ["8H"; "6H"; "TD"; "8D"; "JC"; "5C"]
                Seat.North, ["AS"; "QS"; "7S"; "TH"; "7D"; "QC"]
                Seat.East,  ["9S"; "3S"; "2S"; "3H"; "QD"; "6D"]
                Seat.South, ["TS"; "8S"; "5S"; "AC"; "4C"; "3C"]
            ] |> getHands
        let deal = OpenDeal.fromHands Seat.South hands

        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Two deal

        let cards =
            [
                "AC"
            ] |> Seq.map Card.fromString
        let deal =
            (deal, cards)
                ||> Seq.fold (fun acc card ->
                    OpenDeal.addPlay card acc)
        printfn "%s" (OpenDeal.toString deal)

    let allPass () =

        let hands =
            [
                Seat.West,  ["8H"; "6H"; "TD"; "8D"; "JC"; "5C"]
                Seat.North, ["AS"; "QS"; "7S"; "TH"; "7D"; "QC"]
                Seat.East,  ["9S"; "3S"; "2S"; "3H"; "QD"; "6D"]
                Seat.South, ["TS"; "8S"; "5S"; "AC"; "4C"; "3C"]
            ] |> getHands
        let deal = OpenDeal.fromHands Seat.South hands

        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal

        printfn "%s" (OpenDeal.toString deal)

    System.Console.OutputEncoding <- System.Text.Encoding.Unicode
    onePlay ()
    allPass ()

