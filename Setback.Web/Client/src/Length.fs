namespace Setback.Web.Client

open System

/// CSS length. E.g. "100px".
[<StructuredFormatDisplay("{String}")>]
type Length =
    | Pixel of float

    /// Number of pixels in the length.
    member this.NumPixels =
        let (Pixel px) = this in px

    /// CSS string for this length.
    member this.String =
        sprintf "%Apx" this.NumPixels

    /// CSS string for this length.
    override this.ToString() =
        this.String

    /// Length addition.
    static member (+) (a : Length, b : Length) =
        Pixel (a.NumPixels + b.NumPixels)

    /// Length subtraction.
    static member (-) (a : Length, b : Length) =
        Pixel (a.NumPixels - b.NumPixels)

    /// Scalar length multiplication.
    static member (*) (n : float, len : Length) =
        Pixel (n * len.NumPixels)

    /// Scalar length division.
    static member (/) (len : Length, n : float) =
        Pixel (len.NumPixels / n)

module Length =

    /// Parses a CSS length string.
    let parse (str : string) =
        let suffix = "px"
        assert(str.EndsWith(suffix))
        str.Substring(0, str.Length - suffix.Length)
            |> Double.Parse
            |> Pixel

    /// Gets the length of the given property of the given
    /// jQuery element.
    let ofElement propertyName (elem : JQueryElement) =
        elem.css propertyName |> parse
