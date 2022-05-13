namespace Setback.Web.Client

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
    let run duration (step : AnimationStep) =

        let rec loop (acc : JQueryElement) = function
            | [] -> acc.promise()
            | elemAction :: elemActions ->
                ElementAction.run duration elemAction
                let acc' = acc.add(elemAction.Element)
                loop acc' elemActions

        step
            |> Seq.toList
            |> loop JQueryElement.empty

/// A sequence of steps to be animated.
type Animation = seq<AnimationStep>

module Animation =

    /// Duration of each step, in milliseconds.
    let duration = 200

    /// Runs an animation.
    let run (animation : Animation) =
        let rec loop = function
            | [] -> promise { return () }
            | step :: steps ->
                promise {
                    do! AnimationStep.run duration step
                    return! loop steps
                }
        animation
            |> Seq.toList
            |> loop
