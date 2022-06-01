namespace Setback.Web.Client

/// Surface on which cards are played.
type CardSurface =
    {
        /// Underlying HTML element.
        Element : JQueryElement
    }

module CardSurface =

    /// Initializes a card surface using the given element.
    let init selector =
        {
            Element = ~~selector
        }
