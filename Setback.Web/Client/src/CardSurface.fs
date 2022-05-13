namespace Setback.Web.Client

/// Values from -1.0 to 1.0.
type Coord = float

module Coord =

    /// Converts a -1..1 coordinate to a CSS length, based
    /// on the given maximum length.
    //  -1.0 ->     0 (start of length)
    //   0.0 -> max/2 (center of length)
    //   1.0 ->   max (end of length)
    let toLength (max : Length) (coord : Coord) =
        (0.5 * (float max.NumPixels * (coord + 1.0)))
            |> Pixel

/// Surface on which cards are played. The width and height
/// are constrained so that cards are always fully contained
/// by the surface.
type CardSurface =
    {
        /// Underlying HTML element.
        Element : JQueryElement

        /// Constrained width of the surface.
        Width : Length

        /// Constrained height of the surface.
        Height : Length
    }

module CardSurface =

    /// Initializes a card surface using the given element.
    let init selector =
        let elem = ~~selector
        {
            Element = elem
            Width =
                let width = JQueryElement.length "width" elem
                width - CardView.width - (2.0 * CardView.border)
            Height =
                let height = JQueryElement.length "height" elem
                height - CardView.height - (2.0 * CardView.border)
        }

    /// Converts the given -1..1 coordinates into an HTML
    /// position on the surface.
    let getPosition (x, y) surface =
        let left = Coord.toLength surface.Width x
        let top = Coord.toLength surface.Height y
        Position.create left top
