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
