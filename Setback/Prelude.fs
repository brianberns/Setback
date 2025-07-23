namespace Setback

open System.Collections.Immutable

[<AutoOpen>]
module ImmutableArrayExt =

    type ImmutableArray =
        static member ZeroCreate(length) =
            let array = Array.zeroCreate<'a> length
            ImmutableArray.CreateRange(array)
