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
