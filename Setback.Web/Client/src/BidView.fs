namespace Setback.Web.Client

open Browser.Dom

open PlayingCards
open Setback

/// A view of a bid.
type BidView = JQueryElement

/// Avoid Fable's attempt to invoke illegal constructors.
[<AutoOpen>]
module HTMLElement =

    type HTMLDivElementType() =
        member _.Create() = document.createElement("div")

    let HTMLDivElement = HTMLDivElementType()

    type HTMLButtonElementType() =
        member _.Create() = document.createElement("button")

    let HTMLButtonElement = HTMLButtonElementType()

module BidView =

    /// Creates a bid view.
    let ofBid bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLButtonElement.Create(innerText = innerText)
        bidView.addClass("bid")
        bidView

/// Widget that enables the user to choose a bid.
type BidChooser = JQueryElement

module BidChooser =

    /// Creates a chooser for the given bids, invoking the given
    /// choice handler.
    let create position validBids handler : BidChooser =

            // create an element to hold the bid views
        let div = ~~HTMLDivElement.Create(innerHTML = "<p>Your Bid</p>")
        div.addClass("chooser")
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
                    div.remove()
                    handler bid)
            else
                bidView.addClass("inactive")
            div.append(bidView)

        div
