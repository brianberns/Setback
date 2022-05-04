namespace PlayingCards

open System

[<AutoOpen>]
module Prelude =

    /// Flips the arguments to a function.
    let inline flip f a b = f b a

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
