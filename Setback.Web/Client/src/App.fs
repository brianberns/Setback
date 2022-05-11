namespace Setback.Web.Client

open System

open Fable.Core.JS

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

/// Values from -1.0 to 1.0.
type Coord = float

module Coord =

    let toLength (max : Length) (coord : Coord) =
        (0.5 * (float max.NumPixels * (coord + 1.0)))
            |> Pixel

type CardSurface =
    {
        Element : JQueryElement
        Width : Length
        Height : Length
    }

module CardSurface =

    let init selector =
        let elem = JQuery.select selector
        {
            Element = elem
            Width =
                let width = JQueryElement.length "width" elem
                width - CardView.width - (2.0 * CardView.border)
            Height =
                let height = JQueryElement.length "height" elem
                height - CardView.height - (2.0 * CardView.border)
        }

    let getPosition (x, y) surface =
        let left = Coord.toLength surface.Width x
        let top = Coord.toLength surface.Height y
        Position.create left top

module DealView =

    let incr = 0.1

    let getCoord i =
        (incr * float i) - (0.5 * (6.0 - 1.0) * incr)

    let create surface : Animation =

        let createInstr (x, y) cardOffset iCard cv =
            let pos =
                CardSurface.getPosition (getCoord (iCard + cardOffset) + x, y) surface
            AnimationInstruction.create cv pos true

        let west batch1 batch2 =
            let instr = createInstr (-0.7, 0.0)
            let step1 = batch1 |> Seq.mapi (instr 0)
            let step2 = batch2 |> Seq.mapi (instr 3)
            step1, step2

        let north batch1 batch2 =
            let instr = createInstr (0.0, -0.9)
            let step1 = batch1 |> Seq.mapi (instr 0)
            let step2 = batch2 |> Seq.mapi (instr 3)
            step1, step2

        let east batch1 batch2 =
            let instr = createInstr (0.7, 0.0)
            let step1 = batch1 |> Seq.mapi (instr 0)
            let step2 = batch2 |> Seq.mapi (instr 3)
            step1, step2

        let south batch1 batch2 =
            let instr = createInstr (0.0, 0.9)
            let step1 = batch1 |> Seq.mapi (instr 0)
            let step2 = batch2 |> Seq.mapi (instr 3)
            step1, step2

        let deck =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init 52 (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

        let stepW1, stepW2 = west deck.[0..2] deck.[12..14]
        let stepN1, stepN2 = north deck.[3..5] deck.[15..17]
        let stepE1, stepE2 = east deck.[6..8] deck.[18..20]
        let stepS1, stepS2 = south deck.[9..11] deck.[21..23]
        [
            stepW1; stepN1; stepE1; stepS1
            stepW2; stepN2; stepE2; stepS2
        ]

module App =

    let run () =
        CardSurface.init "#surface"
            |> DealView.create
            |> Animation.run

    (~~document).ready (fun () ->
        run ())
