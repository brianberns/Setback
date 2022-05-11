﻿namespace Setback.Web.Client

open Fable.Core
open Fable.Core.JsInterop

/// jQuery API.
type JQueryElement =

    /// Adds the given class to the element.
    abstract addClass : className : string -> unit

    /// Animates the element towards the given CSS properties.
    abstract animate : properties : obj * ?duration : int -> unit

    /// Appends the given content to the element.
    abstract append : content : JQueryElement -> unit

    /// Handles the element's click event.
    abstract click : handler : (unit -> unit) -> unit

    /// Gets value of the element's given CSS property.
    abstract css : propertyName : string -> string

    /// Sets the element's given CSS properties.
    abstract css : properties : obj -> unit

    /// Handles an element's ready event.
    abstract ready : handler : (unit -> unit) -> unit

module JQuery =

    /// Imports jQuery library.
    let init () =
        importDefault<unit> "jquery"

    /// Selects a jQuery element.
    [<Emit("$($0)")>]
    let select (_selector : obj) : JQueryElement = jsNative

[<AutoOpen>]
module JQueryExt =

    /// Selects a jQuery element.
    let (~~) (selector : obj) =
        JQuery.select selector

type Position =
    {
        Left : Length
        Top : Length
    }

module Position =

    let create left top =
        { Left = left; Top = top }

module JQueryElement =

    /// Gets the length of the given property of the given
    /// jQuery element.
    let length propertyName (elem : JQueryElement) =
        elem.css propertyName |> Length.parse

    module private Position =
        let toCss pos =
            {| left = pos.Left; top = pos.Top |}

    /// Sets the position of the given element instantly.
    let setPosition pos (elem : JQueryElement) =
        pos
            |> Position.toCss
            |> elem.css

    /// Sets and animates the position of the given element.
    let animateTo pos (elem : JQueryElement) =
        pos
            |> Position.toCss
            |> elem.animate
