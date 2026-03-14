namespace Setback

module Array =

    let tryMinBy projection array =
        if Array.length array > 0 then
            array
                |> Array.minBy projection
                |> Some
        else None

    let tryMaxBy projection array =
        if Array.length array > 0 then
            array
                |> Array.maxBy projection
                |> Some
        else None

    let tryMin array = tryMinBy id array

    let tryMax array = tryMaxBy id array

/// Option computation expression builder.
type OptionBuilder() =
    member _.Bind(opt, f) = Option.bind f opt
    member _.Return(x) = Some x
    member _.ReturnFrom(opt : Option<_>) = opt
    member _.Zero() = None

[<AutoOpen>]
module OptionBuilder =

    /// Option computation expression builder.
    let option = OptionBuilder()
