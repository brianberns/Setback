namespace Setback.Web.Client

open Browser.Dom
open Setback

/// A view of a bid.
type BidView = JQueryElement

module BidView =

    /// Creates a bid view.
    let ofBid bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLParagraphElement.Create(innerText = innerText)
        bidView.addClass("bid")
        bidView.css
            {|
                position = "absolute"
            |}
        bidView
