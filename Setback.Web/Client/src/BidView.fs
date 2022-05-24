namespace Setback.Web.Client

open Browser.Dom

open Fable.Core

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

/// A view of a single bid. E.g. "Three".
type BidView = JQueryElement

module BidView =

    /// Creates a clickable bid view.
    let createClickable bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLButtonElement.Create(
                ``type`` = "button",
                innerText = innerText)
        bidView.addClass("bid")
        bidView

    /// Creates an unclickable bid view.
    let createStatic bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLDivElement.Create(
                innerText = innerText)
        bidView.addClass("bid")
        bidView

/// Widget that enables the user to choose a legal bid.
type BidChooser = JQueryElement

module BidChooser =

    /// Creates a chooser for the given bids.
    let create legalBids (handler : Bid -> JS.Promise<_>) : BidChooser * _ =

            // create an element to hold the bid views
        let div = ~~HTMLDivElement.Create(innerHTML = "<p>Your Bid?</p>")
        div.addClass("chooser")

            // enable user to select a bid
        assert(legalBids |> Set.isEmpty |> not)
        let promise =
            Promise.create (fun resolve _reject ->
                for bid in Enum.getValues<Bid> do
                    let bidView = BidView.createClickable bid
                    if legalBids.Contains(bid) then
                        bidView.addClass("active")
                        bidView.click(fun () ->

                                // prevent further clicks
                            div.remove()

                                // handle the bid
                            promise {
                                let! value = handler bid
                                resolve value
                            } |> ignore)
                    else
                        bidView.prop("disabled", true)
                    div.append(bidView))

        div, promise
