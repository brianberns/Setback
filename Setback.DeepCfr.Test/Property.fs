namespace Setback.DeepCfr.Test

open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit

open PlayingCards
open Setback
open Setback.DeepCfr.Model

module Card =

    let ofIndex iCard =
        Card.allCards[iCard]

module Property =

    [<Property>]
    let ``Card index`` card =
        card
            |> Card.toIndex
            |> Card.ofIndex
            = card

    let decodeCards (encoded : _[]) =
        [0 .. encoded.Length - 1]
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

(*

    let decodeTrick encoded =
        for iCard = Seat.numSeats - 1 downto 0 do
            let encodedCard = encoded[iCard]

    [<Property>]
    let ``Decode trick`` isCurrent trickOpt =
        trickOpt
            |> Encoding.encodeTrick
            |> decodeTrick
            = trickOpt
*)

    [<assembly: Properties(
        Arbitrary = [| |])>]
    do ()
