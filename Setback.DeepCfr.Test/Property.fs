namespace Setback.DeepCfr.Test

open FsCheck.FSharp
open FsCheck.Xunit

open PlayingCards
open Setback
open Setback.DeepCfr.Model

module Gen =

    let one<'t> =
        let arb = ArbMap.arbitrary<'t> ArbMap.defaults
        arb.Generator

module Enum =

    let gen<'t> =
        gen {
            let values = Enum.getValues<'t>
            let! i = Gen.choose (0, values.Length - 1)
            return values[i]
        }

module Card =

    let ofIndex iCard =
        Card.allCards[iCard]

module Playout =

    let private toHandMap bidder cards =
        cards
            |> Seq.indexed
            |> Seq.groupBy (fun (iCard, _) ->
                let n = iCard % Seat.numSeats
                bidder |> Seat.incr n)
            |> Seq.map (fun (seat, group) ->
                let cards =
                    group
                        |> Seq.map snd<_, Card>
                        |> set
                seat, cards)
            |> Map

    let rec private genPlay nCards (handMap : Map<_, _>) playout =
        gen {
            if nCards > 0 then
                let player = Playout.currentPlayer playout
                let hand = handMap[player]
                let legalPlays =
                    Playout.legalPlays hand playout
                        |> Seq.toArray
                let! iCard = Gen.choose (0, legalPlays.Length - 1)
                let card = legalPlays[iCard]
                let playout = Playout.addPlay card playout
                let hand = Set.remove card hand
                let handMap = Map.add player hand handMap
                return! genPlay (nCards - 1) handMap playout
            else
                return playout
        }

    let gen =
        gen {
            let! bidder = Enum.gen<Seat>
            let! deck = Gen.shuffle Card.allCards
            let handMap =
                deck
                    |> Seq.take Setback.numCardsPerDeal
                    |> toHandMap bidder
            let playout = Playout.create bidder
            let! nCards = Gen.choose (0, Setback.numCardsPerDeal)
            return! genPlay nCards handMap playout
        }

type Arbs =
    static member Playout() = Arb.fromGen Playout.gen

module Property =

    [<Property>]
    let ``Card index`` card =
        card
            |> Card.toIndex
            |> Card.ofIndex
            = card

    let decodeCards (encoded : _[]) =
        [ 0 .. encoded.Length - 1 ]
            |> Seq.where (fun iCard -> encoded[iCard])
            |> Seq.map Card.ofIndex
            |> Seq.toArray

    let decodeCardOpt encoded =
        match decodeCards encoded with
            | [||] -> None
            | [| card |] -> Some card
            | _ -> failwith "Too many cards"

    [<Property>]
    let ``Decode card`` cardOpt =
        cardOpt
            |> Option.toArray
            |> Encoding.encodeCards
            |> decodeCardOpt
            = cardOpt

    [<assembly: Properties(
        Arbitrary = [| typeof<Arbs> |],
        Verbose = false,
        MaxTest = 1000)>]
    do ()
