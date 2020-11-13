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

    /// Answers the least of all the items in the given sequence by
    /// the given projection, if any.
    let tryMinBy projection (items : seq<_>) =
        use e = items.GetEnumerator()
        if e.MoveNext() then
            let mutable minItem = e.Current
            let mutable minValue = projection minItem
            while e.MoveNext() do
                let value = projection e.Current
                if value < minValue then
                    minItem <- e.Current
                    minValue <- value
            Some minItem
        else
            None

    /// Answers the greatest of all the items in the given sequence by
    /// the given projection, if any.
    let tryMaxBy projection (items : seq<_>) =
        use e = items.GetEnumerator()
        if e.MoveNext() then
            let mutable maxItem = e.Current
            let mutable maxValue = projection maxItem
            while e.MoveNext() do
                let value = projection e.Current
                if value > maxValue then
                    maxItem <- e.Current
                    maxValue <- value
            Some maxItem
        else
            None

module Option =

    /// Unzips the given tuple option into a tuple of options.
    let unzip = function
        | Some (a, b) -> Some a, Some b
        | None -> None, None

module Char =

    /// Decimal digits.
    let private decDigits = "0123456789"

    /// Hexadecimal digits.
    let private hexDigits = "0123456789ABCDEF"

    /// Converts the given decimal digit to a single character.
    let fromDigit digit =
        decDigits.[digit]

    /// Converts the given decimal digit to a single character.
    let fromHexDigit digit =
        hexDigits.[digit]
