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

type AnimationInstruction =
    {
        Element : JQueryElement
        Target : Coord * Coord
        BringToFront : bool
    }

module AnimationInstruction =

    let create elem target bringToFront =
        {
            Element = elem
            Target = target
            BringToFront = bringToFront
        }

    let run surface instr =
        if instr.BringToFront then
            instr.Element |> JQueryElement.bringToFront
        let pos =
            surface
                |> CardSurface.getPosition instr.Target
        instr.Element
            |> JQueryElement.animateTo pos

type AnimationStep = seq<AnimationInstruction>

module AnimationStep =

    let run surface step =
        for instr in step do
            AnimationInstruction.run surface instr

type Animation = List<AnimationStep>

module Animation =

    let run surface (animation : Animation) =
        let rec loop = function
            | [] -> ()
            | (step : AnimationStep) :: steps ->
                AnimationStep.run surface step
                let callback () = loop steps
                setTimeout callback 500 |> ignore
        loop animation

module App =

    let run () =

        let surface = CardSurface.init "#surface"

        let deck =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init 6 (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

        let incr = 0.1
        let getX i =
            (incr * float i) - (0.5 * 6.0 * incr)

        let animation : Animation =
            [
                [
                    AnimationInstruction.create deck.[0] (getX 0, 0.9) true
                    AnimationInstruction.create deck.[1] (getX 1, 0.9) true
                    AnimationInstruction.create deck.[2] (getX 2, 0.9) true
                ]
                [
                    AnimationInstruction.create deck.[3] (getX 3, 0.9) true
                    AnimationInstruction.create deck.[4] (getX 4, 0.9) true
                    AnimationInstruction.create deck.[5] (getX 5, 0.9) true
                ]
            ]

        Animation.run surface animation

    (~~document).ready (fun () ->
        run ())
