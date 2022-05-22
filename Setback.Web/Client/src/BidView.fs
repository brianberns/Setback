namespace Setback.Web.Client

open Browser.Dom

open PlayingCards
open Setback

/// Avoid Fable's attempt to invoke illegal constructors.
[<AutoOpen>]
module HTMLElement =

    open Browser.Types

    type HTMLDivElementType() =
        member _.Create() =
            document.createElement("div")
                :?> HTMLDivElement

    let HTMLDivElement = HTMLDivElementType()

    type HTMLButtonElementType() =
        member _.Create() =
            document.createElement("button")
                :?> HTMLButtonElement

    let HTMLButtonElement = HTMLButtonElementType()

/// A view of a bid.
type BidView = JQueryElement

module BidView =

    /// Creates a clickable bid view.
    let ofBid bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLButtonElement.Create(
                ``type`` = "button",
                innerText = innerText)
        bidView.addClass("bid")
        bidView

/// Widget that enables the user to choose a bid.
type BidChooser = JQueryElement

module BidChooser =

    /// Creates a chooser for the given bids, invoking the given
    /// choice handler.
    let create legalBids handler : BidChooser =

            // create an element to hold the bid views
        let div = ~~HTMLDivElement.Create(innerHTML = "<p>Your Bid?</p>")
        div.addClass("chooser")

            // invoke handler when a valid bid is chosen
        for bid in Enum.getValues<Bid> do
            let bidView = BidView.ofBid bid
            if legalBids |> Set.contains bid then
                bidView.addClass("active")
                bidView.click(fun () ->
                    div.remove()
                    handler bid)
            else
                bidView.prop("disabled", true)
            div.append(bidView)

        div
