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

    let setOfSize genElement size =
        let rec loop (elements : Set<_>) =
            gen {
                if elements.Count >= size then
                    return elements
                else
                    let! element = genElement
                    return! elements.Add(element) |> loop
            }
        loop Set.empty

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

module Trick =

    let gen =
        gen {
            let! leader = Enum.gen<Seat>
            let trick = Trick.create leader
            let! size = Gen.choose (0, Seat.numSeats - 1)
            let! cards =
                Gen.setOfSize Gen.one<Card> size
                    >>= Gen.shuffle
            let! trump = Enum.gen<Suit>
            let trick =
                (trick, cards)
                    ||> Seq.fold (fun trick card ->
                        Trick.addPlay trump card trick)
            return trick, trump
        }

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
            let playout = Playout.create bidder
            let! nCards = Gen.choose (0, Setback.numCardsPerDeal - 1)
            let! cards = Gen.setOfSize Gen.one<Card> nCards
            let handMap = toHandMap bidder cards
            return! genPlay nCards handMap playout
        }

type Arbs =
    static member Trick() = Arb.fromGen Trick.gen
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

    let decodeTrick trump leader (encoded : _[]) =
        let cards =
            [ 0 .. Seat.numSeats - 1 ]
                |> Seq.choose (fun iCard ->
                    let iFrom = iCard * Encoding.encodedCardLength
                    let iTo = (iCard + 1) * Encoding.encodedCardLength - 1
                    decodeCardOpt encoded[iFrom .. iTo])
        let trick = Trick.create leader
        (trick, cards)
            ||> Seq.fold (fun trick card ->
                Trick.addPlay trump card trick)

    [<Property>]
    let ``Decode trick`` (trick, trump) =
        let isCurrent = not (Trick.isComplete trick)
        let actual =
            Some trick
                |> Encoding.encodeTrick isCurrent
                |> decodeTrick trump trick.Leader
        actual = trick

    let decodePlayout bidder (encoded : _[]) =
        let playout = Playout.create bidder
        playout

    [<Property>]
    let ``Decode playout`` playout =
        if Playout.isComplete playout then true   // can't encode a complete playout
        else
            let actual =
                playout
                    |> Encoding.encodePlayout
                    |> decodePlayout playout.Bidder
            actual = playout

    [<assembly: Properties(
        Arbitrary = [| typeof<Arbs> |])>]
    do ()
