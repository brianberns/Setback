namespace Setback.Web.Client

open Fable.Core.JS

/// Animation action.
type AnimationAction =

    /// Move element to position.
    | MoveTo of Position

    /// Bring element to front.
    | BringToFront

    /// Replace element with another element.
    | ReplaceWith of JQueryElement

    /// Remove element.
    | Remove

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
    let run duration elemAction =
        match elemAction.Action with
            | MoveTo pos ->
                elemAction.Element
                    |> JQueryElement.animateTo pos duration
            | BringToFront ->
                elemAction.Element
                    |> JQueryElement.bringToFront
            | ReplaceWith replacementElem ->
                elemAction.Element
                    |> JQueryElement.replaceWith replacementElem
            | Remove ->
                elemAction.Element.remove()

/// One step in an animation. All the actions in a step
/// are animated simultaneously in parallel.
type AnimationStep = seq<ElementAction>

module AnimationStep =

    /// Runs an animation step.
    let run duration =
        Seq.iter (ElementAction.run duration)

/// A sequence of steps to be animated.
type Animation = List<AnimationStep>

module Animation =

    /// Duration of each step, in milliseconds.
    let duration = 200

    /// Runs an animation.
    let run (animation : Animation) =
        let rec loop = function
            | [] -> ()
            | (step : AnimationStep) :: steps ->
                AnimationStep.run (duration - 20) step   // make sure each step finishes before next step starts
                let callback () = loop steps
                setTimeout callback duration |> ignore
        loop animation
