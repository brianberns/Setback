namespace Setback.Web.Client

/// Animation action.
type AnimationAction =

    /// Move element to position.
    | MoveTo of Position * duration : int

    /// Bring element to front.
    | BringToFront

    /// Replace element with another element.
    | ReplaceWith of JQueryElement

    /// Remove element.
    | Remove

module AnimationAction =

    /// Default duration of a move, in milliseconds.
    let private duration = 300

    /// Moves to the given position with the default duration.
    let moveTo position =
        MoveTo (position, duration)

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
            | MoveTo (pos, duration) ->
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
        elemAction.Element.promise()

/// Describes an animation.
[<RequireQualifiedAccess>]
type Animation =

    /// Animation of a single element action.
    | Unit of ElementAction

    /// Parallel animation.
    | Parallel of Animation[]

    /// Serial animation.
    | Serial of Animation[]

    /// Sleeps for the given number of milliseconds.
    | Sleep of duration : int

module Animation =

    /// Creates an animation unit.
    let create elem action =
        ElementAction.create elem action
            |> Animation.Unit

    /// Runs an animation.
    let rec run = function
        | Animation.Unit elemAction ->
            ElementAction.run elemAction
        | Animation.Parallel anims ->
            anims
                |> Seq.map run
                |> Promise.all
                |> Promise.map (fun _ -> ())
        | Animation.Serial anims ->
            promise {
                for anim in anims do
                    do! run anim
            }
        | Animation.Sleep duration ->
            Promise.sleep duration
