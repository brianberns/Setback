namespace Setback.Web.Client

open Fable.Core
open Fable.Core.JsInterop

/// CSS length percentage. E.g. "50%".
[<StructuredFormatDisplay("{String}")>]
type LengthPercentage = Percent of float with

    /// Display string.
    member this.String =
        let (Percent pct) = this
        $"{pct}%%"

    /// Display string.
    override this.ToString() =
        this.String

    /// Addition.
    static member (+) (Percent a, Percent b) =
        Percent (a + b)

/// CSS-compatible percentage position.
type Position =
    {
        /// X coordinate.
        left : LengthPercentage

        /// Y coordinate.
        top : LengthPercentage
    } with

    /// Creates a position.
    static member Create(left, top) =
        {
            left = left
            top = top
        }

    /// Addition.
    static member (+) (a, b) =
        Position.Create(
            a.left + b.left,
            a.top + b.top)

module Position =

    /// Creates a position.
    let ofFloats (left, top) =
        Position.Create (Percent left, Percent top)

    /// Creates a position.
    let ofInts (left, top) =
        ofFloats (float left, float top)

    /// Creates a map of positions.
    let seatMap seatPairs =
        seatPairs
            |> Seq.map (fun (seat, pair) ->
                seat, ofInts pair)
            |> Map

/// jQuery API.
type JQueryElement =

    /// Adds the given class to the element.
    abstract addClass : className : string -> unit

    /// Animates the element towards the given CSS properties.
    abstract animate : properties : obj * ?duration : int -> unit

    /// Appends the given content to the element.
    abstract append : content : JQueryElement -> unit

    /// Gets the element's attribute of the given name.
    abstract attr : attributeName : string -> string

    /// Sets the element's attribute of the given name.
    abstract attr : attributeName : string * value : string -> unit

    /// Handles the element's change event.
    abstract change : handler : (unit -> unit) -> unit

    /// Handles the element's click event.
    abstract click : handler : (unit -> unit) -> unit

    /// Gets value of the element's given CSS property.
    abstract css : propertyName : string -> string

    /// Sets the element's given CSS properties.
    abstract css : properties : obj -> unit

    /// Gets the underlying DOM element(s).
    abstract get : unit -> Browser.Types.HTMLElement[]

    /// Removes an event handler.
    abstract off : eventName : string -> unit

    /// Gets the element's parent.
    abstract parent : unit -> JQueryElement

    /// Creates a promise that will be resolved when element
    /// animation is complete.
    abstract promise : unit -> JS.Promise<unit>

    /// Sets the element's property of the given name.
    abstract prop : propertyName : string * value : obj -> unit

    /// Handles an element's ready event.
    abstract ready : handler : (unit -> unit) -> unit

    /// Removes the element from the DOM.
    abstract remove : unit -> unit

    /// Removes the given class from the element.
    abstract removeClass : className : string -> unit

    /// Sets the element's text contents.
    abstract text : text : string -> unit

    /// Gets the element's value.
    abstract ``val`` : unit -> int

module JQueryElement =

    open Browser

    /// Imports jQuery library.
    let init () =
        importDefault<unit> "jquery"

    /// Selects a jQuery element.
    [<Emit("$($0)")>]
    let select (_selector : obj) : JQueryElement = jsNative

    /// Increments and returns the next z-index.
    let zIndexIncr =
        let mutable zIndex = 0
        fun () ->
            zIndex <- zIndex + 1
            zIndex

    /// Gets the z-index of the given element.
    let private getZIndex (elem : JQueryElement) =
        elem.css("z-index")

    /// Sets the z-index of the given element.
    let private setZIndex zIndex (elem : JQueryElement) =
        elem.css {| ``z-index`` = zIndex |}

    /// Brings the given card view to the front.
    let bringToFront (elem : JQueryElement) =
        let zIndex = zIndexIncr ()
        setZIndex zIndex elem

    /// Sets the position of the given element.
    let setPosition (pos : Position) (elem : JQueryElement) =
        elem.css pos

    /// Sets and animates the position of the given element.
    let animateTo (pos : Position) duration (elem : JQueryElement) =
        elem.animate(pos, duration)

    /// Replaces one element with another.
    let replaceWith replacementElem elem =

            // use same z-index
        replacementElem
            |> setZIndex (getZIndex elem)

            // use same position (https://stackoverflow.com/a/18297116/344223)
        let style = elem.get().[0].style
        replacementElem.css
            {|
                left = style.left
                top = style.top
            |}

            // switch elements
        let parent = elem.parent()
        parent.append(replacementElem)
        elem.remove()

[<AutoOpen>]
module JQueryExt =

    /// Selects a jQuery element.
    let (~~) (selector : obj) =
        JQueryElement.select selector
