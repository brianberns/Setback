namespace Setback.Web.Client

open System

/// CSS length. E.g. "100px".
[<StructuredFormatDisplay("{String}")>]
type Length =
    | Pixel of float

    member this.NumPixels =
        let (Pixel px) = this
        px

    member this.String =
        sprintf "%Apx" this.NumPixels

    override this.ToString() =
        this.String

    static member (+) (a : Length, b : Length) =
        Pixel (a.NumPixels + b.NumPixels)

    static member (-) (a : Length, b : Length) =
        Pixel (a.NumPixels - b.NumPixels)

    static member (*) (n : float, len : Length) =
        Pixel (n * len.NumPixels)

    static member (/) (len : Length, n : float) =
        Pixel (len.NumPixels / n)

module Length =

    let parse (str : string) =
        let suffix = "px"
        assert(str.EndsWith(suffix))
        str.Substring(0, str.Length - suffix.Length)
            |> Double.Parse
            |> Pixel
