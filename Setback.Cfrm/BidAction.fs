namespace Setback.Cfrm

open PlayingCards
open Setback

/// An action that applies during an auction.
type BidAction = BidAction of Bid

module BidAction =

    /// Compares two arrays using "alphabetical" order.
    let private compareArrays (itemsA : _[]) (itemsB : _[]) =
        let minLength = min itemsA.Length itemsB.Length
        let rec loop index =
            if index = minLength then
                compare itemsA.Length itemsB.Length
            else
                let itemA = itemsA.[index]
                let itemB = itemsB.[index]
                match compare itemA itemB with
                    | 0 -> loop (index + 1)
                    | value -> value
        loop 0

    /// Chooses the best trump ranks from the given hand, sorted
    /// in descending order.
    let chooseTrumpRanks (hand : Hand) =

        let pairs =
            hand
                |> Seq.groupBy (fun card -> card.Suit)
                |> Seq.map (fun (suit, cards) ->
                    let strength =
                        cards
                            |> Seq.sumBy (fun card ->
                                int card.Rank - 1)
                    let ranks =
                        cards
                            |> Seq.map (fun card -> card.Rank)
                            |> Seq.sortDescending
                            |> Seq.toArray
                    strength, (suit, ranks))
                |> Seq.sortWith (fun (strengthA, (_, ranksA)) (strengthB, (_, ranksB)) ->
                    let value =
                        match compare strengthA strengthB with
                            | 0 -> compareArrays ranksA ranksB
                            | value -> value
                    -1 * value)
                |> Seq.toArray
        assert(pairs.Length > 0)

        [|
            let strength0, (suit0, ranks0) = pairs.[0]
            yield suit0, ranks0

            if pairs.Length > 1 then
                let strength1, (suit1, ranks1) = pairs.[1]
                assert(strength0 > strength1
                    || (strength0 = strength1 && compareArrays ranks0 ranks1 >= 0))
                if strength0 - strength1 < 2
                    && compare ranks0 ranks1 <> 0 then
                    yield suit1, ranks1
        |]

    /// Actions available in the given situation, sorted for
    /// reproducibility.
    let getActions hand (auction : AbstractAuction) =
        let hasJack =
            hand
                |> chooseTrumpRanks
                |> Seq.collect snd
                |> Seq.contains Rank.Jack
        auction
            |> AbstractAuction.legalBids
            |> Seq.where (fun bid ->
                bid <> Bid.Four || hasJack)   // don't consider four-bid without Jack
            |> Seq.map BidAction
            |> Seq.toArray

    /// Extracts bid from a bid action.
    let getBid auction (BidAction bid) =
        assert(
            auction
                |> AbstractAuction.legalBids
                |> Seq.contains bid)
        bid
