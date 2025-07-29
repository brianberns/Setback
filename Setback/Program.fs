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

    let test1 () =

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

    let test2() =

        let hands =
            [
                Seat.West,  ["JS"; "3H"; "KC"; "8C"; "3C"; "9D"]
                Seat.North, ["9S"; "4S"; "TC"; "9C"; "8D"; "6D"]
                Seat.East,  ["TS"; "8S"; "KH"; "6H"; "2H"; "QC"]
                Seat.South, ["KS"; "5S"; "9H"; "8H"; "3D"; "2D"]
            ] |> getHands
        let deal = OpenDeal.fromHands Seat.North hands

        let deal = OpenDeal.addBid Bid.Three deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal
        let deal = OpenDeal.addBid Bid.Pass deal

        let deal =
            (deal, [
                "KH"; "9H"; "3H"; "6D";
                "8S"; "KS"; "JS"; "4S";
                "8H"; "3C"; "8D";
            ]) ||> Seq.fold (fun deal name ->
                OpenDeal.addPlay (Card.fromString name) deal)
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
    test1 ()
    printfn "------------------------------------------------"
    test2 ()
    printfn "------------------------------------------------"
    allPass ()
