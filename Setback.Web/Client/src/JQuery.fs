namespace Setback.Web.Client

open Fable.Core
open Fable.Core.JsInterop

/// jQuery API.
type JQueryElement =

    abstract addClass : className : string -> unit

    /// Animates towards the given CSS properties.
    abstract animate : properties : obj -> unit

    abstract append : content : JQueryElement -> unit

    /// Handles a click event.
    abstract click : handler : (unit -> unit) -> unit

    /// Gets value of CSS property.
    abstract css : propertyName : string -> string

    abstract css : properties : obj -> unit

    /// Handles a document's ready event.
    abstract ready : handler : (unit -> unit) -> unit

module JQuery =

    /// Imports jQuery library.
    let init () =
        importDefault<unit> "jquery"

    /// Selects a jQuery element.
    [<Emit("$($0)")>]
    let select (_selector : obj) : JQueryElement = jsNative
