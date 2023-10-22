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
        bidView.attr("data-bid", Bid.toString bid)
        bidView.addClass("bid")
        bidView.addClass("bid-clickable")
        bidView

    /// Creates an unclickable bid view.
    let createStatic bid : BidView =
        let bidView =
            let innerText = Bid.toString bid
            ~~HTMLDivElement.Create(
                innerText = innerText)
        bidView.attr("data-bid", Bid.toString bid)
        bidView.addClass("bid")
        bidView.addClass("bid-static")
        bidView

    /// Creates a trump bid view.
    let createTrump bid suit : BidView =
        let bidView =
            let innerHTML =
                let suitChar = Suit.toChar suit
                $"{Bid.toString bid} <span class={suitChar}>{suitChar}</span>"
            ~~HTMLDivElement.Create(
                innerHTML = innerHTML)
        bidView.attr("data-bid", Bid.toString bid)
        bidView.addClass("bid")
        bidView.addClass("bid-static")
        bidView.addClass("trump")
        bidView

    /// Answers the given bid view's underlying bid.
    let bid (bidView : BidView) =
        bidView.attr("data-bid")
            |> Bid.fromString

/// Widget that enables the user to choose a legal bid.
type BidChooser =
    {
        /// Underlying HTML element.
        Element : JQueryElement

        /// View for each bid.
        BidViews : BidView[]
    }

module BidChooser =

    /// Creates a chooser.
    let create () =

            // create an element to hold the bid views
        let div = ~~HTMLDivElement.Create(innerHTML = "<p>Your Bid?</p>")
        div.addClass("bid-chooser")

            // create bid views
        let bidViews =
            Enum.getValues<Bid>
                |> Array.map BidView.createClickable
        for bidView in bidViews do
            div.append(bidView)

        {
            Element = div
            BidViews = bidViews
        }

    /// Makes the given chooser visible.
    let display chooser =
        chooser.Element.css {| display = "block" |}