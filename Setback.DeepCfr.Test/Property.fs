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

    let decodeCardOpt (encoded : bool[]) =
        let cardIdxs =
            [0 .. encoded.Length - 1]
                |> Seq.where (fun iCard -> encoded[iCard])
                |> Seq.toArray
        match cardIdxs  with
            | [||] -> None
            | [| iCard |] -> Some (Card.ofIndex iCard)
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

    [<Property>]
    let ``Card index`` card =
        card
            |> Card.toIndex
            |> Card.ofIndex
            = card

    [<assembly: Properties(
        Arbitrary = [| |])>]
    do ()
