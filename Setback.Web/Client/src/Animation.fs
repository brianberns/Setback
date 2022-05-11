namespace Setback.Web.Client

open Fable.Core.JS

type AnimationInstruction =
    {
        Element : JQueryElement
        Target : Position
        BringToFront : bool
    }

module AnimationInstruction =

    let create elem target bringToFront =
        {
            Element = elem
            Target = target
            BringToFront = bringToFront
        }

    let run instr =
        if instr.BringToFront then
            instr.Element |> JQueryElement.bringToFront
        instr.Element
            |> JQueryElement.animateTo instr.Target

type AnimationStep = seq<AnimationInstruction>

module AnimationStep =

    let run step =
        for instr in step do
            AnimationInstruction.run instr

type Animation = List<AnimationStep>

module Animation =

    let run (animation : Animation) =
        let rec loop = function
            | [] -> ()
            | (step : AnimationStep) :: steps ->
                AnimationStep.run step
                let callback () = loop steps
                setTimeout callback 500 |> ignore
        loop animation
