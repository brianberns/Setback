namespace Setback

open Avalonia.Controls
open Avalonia.FuncUI.DSL

type Model = Dummy

module Client =

    let init () = Dummy

    let update () model = model

    /// Creates a view of the given model.
    let view model dispatch =
        DockPanel.create [
        ]
