namespace PlayingCards

open System

module Enum =

    /// Answers all values of the given enum type.
    let getValues<'enum> =
        Enum.GetValues(typeof<'enum>)
            |> Seq.cast<'enum>
            |> Seq.toArray

module Array =

    /// Clones the given array.
    let clone (items : 'item[]) =
        items.Clone()
            :?> 'item[]
