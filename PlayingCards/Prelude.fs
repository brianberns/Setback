namespace PlayingCards

open System

module Enum =

    /// Answers all values of the given enum type.
    let inline getValues<'enum> =
        Enum.GetValues(typeof<'enum>)
            |> Seq.cast<'enum>
            |> Seq.toArray

module Array =

    /// Clones the given array.
    let clone (items : 'item[]) =
#if FABLE_COMPILER
        items
            |> Seq.readonly   // force a copy
            |> Seq.toArray
#else
        items.Clone()
            :?> 'item[]
#endif

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
