namespace Setback.Cfrm

#if !FABLE_COMPILER
open System
#endif

/// Defines the layout of a span.
type SpanLayout<'t> =
    {
        /// Length of span.
        Length : int

        /// Positions of child layouts within span.
        ChildPositions : (int * SpanLayout<'t>)[]
    }

    /// Answers the slice of the given span that corresponds to
    /// the given child layout.
    member layout.Slice(iChild, span : Span<'t>) =
        let start, child = layout.ChildPositions[iChild]
        span.Slice(start, child.Length)

module SpanLayout =

    /// Creates a leaf layout of the given length.
    let ofLength length =
        {
            Length = length
            ChildPositions = Array.empty
        }

    /// Combines the given child layouts in a new layout.
    let combine children =

            // find the position of each child
        let positions =
            (0, children)
                ||> Array.scan (fun len child ->
                    len + child.Length)
        assert(positions.Length = children.Length + 1)

        {
                // total length of all children
            Length =
                positions |> Array.last

                // match children to their positions
            ChildPositions =
                Seq.zip positions children
                    |> Seq.toArray
        }
