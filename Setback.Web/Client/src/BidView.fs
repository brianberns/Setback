namespace Setback.Web.Client

open Browser.Dom

open PlayingCards
open Setback

/// A view of a bid.
type BidView = JQueryElement

/// Avoid Fable's attempt to invoke illegal constructors.
[<AutoOpen>]
module HTMLElement =

    type HTMLParagraphElementType() =
        member _.Create() = document.createElement("p")

    let HTMLParagraphElement = HTMLParagraphElementType()

    type HTMLDivElementType() =
        member _.Create() = document.createElement("div")

    let HTMLDivElement = HTMLDivElementType()

module BidView =

    /// Creates a bid view.
    let ofBid bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLParagraphElement.Create(innerText = innerText)
        bidView.addClass("bid")
        bidView

type BidChooser = JQueryElement

module BidChooser =

    /// Creates a bid chooser for the given bids, with the
    /// given choice handler.
    let create position validBids handler : BidChooser =

            // create an element to hold the bid views
        let div = ~~HTMLDivElement.Create()
        div.css
            {|
                position = "absolute"
            |}
        JQueryElement.setPosition position div

            // invoke handler when a valid bid is chosen
        for bid in Enum.getValues<Bid> do
            let bidView = BidView.ofBid bid
            if validBids |> Set.contains bid then
                bidView.addClass("active")
                bidView.click(fun () ->
                    handler bid)
            div.append(bidView)

        div
