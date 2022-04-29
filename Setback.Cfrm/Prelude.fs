namespace Setback.Cfrm

/// https://stackoverflow.com/questions/62082930/best-way-to-do-trymax-and-trymin-in-f
module Seq =

    /// Answers the least of all items in the given sequence, if any
    let tryMin (items : seq<_>) =
        use e = items.GetEnumerator()
        if e.MoveNext() then
            let mutable result = e.Current
            while e.MoveNext() do
                result <- min e.Current result
            Some result
        else None

module Option =

    /// Unzips the given tuple option into a tuple of options.
    let unzip = function
        | Some (a, b) -> Some a, Some b
        | None -> None, None

module Char =

    /// Converts the given decimal digit to a single character.
    let fromDigit digit =
        "0123456789".[digit]

    /// Converts the given hex digit to a single character.
    let fromHexDigit digit =
        "0123456789ABCDEF".[digit]

#if FABLE_COMPILER

open System.Collections
open System.Collections.Generic

type ImmutableArray<'t>(items : 't[]) =

    let getEnumerator () = (items :> seq<'t>).GetEnumerator()

    new(items : seq<_>) = ImmutableArray(Seq.toArray items)

    interface IEnumerable<'t> with
        member _.GetEnumerator() = getEnumerator ()

    interface IEnumerable with
        member _.GetEnumerator() = getEnumerator () :> IEnumerator

    static member Empty = Array.empty<'t> |> ImmutableArray
    member _.Item(index) = items.[index]
    member _.Length = items.Length
    member _.SetItem(index, item) =
        let items' = PlayingCards.Array.clone items
        items'.[index] <- item
        ImmutableArray(items')

type ImmutableArrayBuilder<'t>(n) =
    let items = ResizeArray(n : int)
    member _.AddRange(range : seq<'t>) = items.AddRange(range)
    member _.Add(item) = items.Add(item)
    member _.ToImmutable() = items |> Seq.toArray |> ImmutableArray

type ImmutableArray =
    static member CreateBuilder<'t>(n) = ImmutableArrayBuilder<'t>(n)
    static member CreateRange<'t>(range : seq<'t>) = ImmutableArray<'t>(range)

#endif
