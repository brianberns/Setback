namespace Setback.Web.Client

open Fable.Core.JS

/// Animation action.
type AnimationAction =

    /// Move element to position.
    | MoveTo of Position

    /// Bring element to front.
    | BringToFront

/// An animation action applied to an element.
type ElementAction =
    {
        /// Element being animated.
        Element : JQueryElement

        /// Action to perform on the element.
        Action : AnimationAction
    }

module ElementAction =

    /// Creates an element action.
    let create elem action =
        {
            Element = elem
            Action = action
        }

    /// Runs an element action.
    let run elemAction =
        match elemAction.Action with
            | MoveTo pos ->
                elemAction.Element
                    |> JQueryElement.animateTo pos
            | BringToFront ->
                elemAction.Element
                    |> JQueryElement.bringToFront

/// One step in an animation.
type AnimationStep = seq<ElementAction>

module AnimationStep =

    /// Runs an animation step.
    let run =
        Seq.iter ElementAction.run

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
