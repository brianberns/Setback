namespace Setback.Web.Client

open Fable.Core.JS

/// One action in an animation step.
type AnimationAction =
    {
        /// Element being animated.
        Element : JQueryElement

        /// Element's target position.
        Target : Position

        /// Bring element to front?
        BringToFront : bool
    }

module AnimationAction =

    /// Creates an animation action.
    let create elem target bringToFront =
        {
            Element = elem
            Target = target
            BringToFront = bringToFront
        }

    /// Runs an animation action.
    let run action =
        if action.BringToFront then
            action.Element |> JQueryElement.bringToFront
        action.Element
            |> JQueryElement.animateTo action.Target


/// One step in an animation.
type AnimationStep = seq<AnimationAction>

module AnimationStep =

    /// Runs an animation step.
    let run (step : AnimationStep) =
        for instr in step do
            AnimationAction.run instr

/// A sequence of steps to be animated.
type Animation = List<AnimationStep>

module Animation =

    /// Runs an animation.
    let run (animation : Animation) =
        let rec loop = function
            | [] -> ()
            | (step : AnimationStep) :: steps ->
                AnimationStep.run step
                let callback () = loop steps
                setTimeout callback 500 |> ignore
        loop animation
