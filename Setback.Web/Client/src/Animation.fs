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

    /// Moves to the given position with the system duration.
    let moveTo position =
        let duration =
            let speed = (~~"#animationSpeed").``val``()
            600 - speed   // duration is inverse of speed
        Browser.Dom.console.log(duration)
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
        let elems =
            match elemAction.Action with
                | MoveTo (pos, duration) ->
                    elemAction.Element
                        |> JQueryElement.animateTo pos duration
                    [| elemAction.Element |]
                | BringToFront ->
                    elemAction.Element
                        |> JQueryElement.bringToFront
                    [| elemAction.Element |]
                | ReplaceWith replacementElem ->
                    elemAction.Element
                        |> JQueryElement.replaceWith replacementElem
                    [| elemAction.Element; replacementElem |]
                | Remove ->
                    elemAction.Element.remove()
                    [| elemAction.Element |]
        elems
            |> Seq.map (fun elem -> elem.promise())
            |> Promise.all
            |> Promise.map (fun _ -> ())

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
