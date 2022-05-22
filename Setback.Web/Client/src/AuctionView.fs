namespace Setback.Web.Client

open Setback

/// View of a each seat's bid in an auction.
module AuctionView =

    /// Bid animation destinations.
    let private destMap =
        Map [
            Seat.West,  Pt (-0.83,  0.6)
            Seat.North, Pt (-0.30, -0.9)
            Seat.East,  Pt ( 0.86,  0.6)
            Seat.South, Pt (-0.30,  1.29)
        ]

    /// Animates the given bid for the given seat.
    let bidAnim surface seat bid =

            // create view of the bid
        let bidView = BidView.createStatic bid
        bidView.css
            {|
                position = "absolute"
                ``z-index`` = JQueryElement.zIndexIncr ()
            |}
        let origin =
            surface |> CardSurface.getPosition Point.origin
        JQueryElement.setPosition origin bidView
        surface.Element.append(bidView)

            // animate the the bid view to its destination
        let dest =
            surface |> CardSurface.getPosition destMap.[seat]
        [|
            AnimationAction.moveTo dest
                |> ElementAction.create bidView
                |> Animation.Unit
            Animation.Sleep 1000
        |] |> Animation.Serial
