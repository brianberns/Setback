namespace Setback.Web.Client

open Browser.Dom

open PlayingCards
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
        bidView

type BidViewChooser = JQueryElement

module BidViewChooser =

    let create position validBids handler : BidViewChooser =

        let div = ~~HTMLDivElement.Create()
        div.css
            {|
                position = "absolute"
            |}
        JQueryElement.setPosition position div

        for bid in Enum.getValues<Bid> do
            let bidView = BidView.ofBid bid
            if validBids |> Set.contains bid then
                bidView.addClass("active")
                bidView.click(fun () ->
                    handler bid)
            div.append(bidView)

        div
