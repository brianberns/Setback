namespace Setback.Web.Client

open Setback

/// View of a each seat's bid in an auction.
module AuctionView =

    /// Bids made in this auction.
    /// ASSUMPTION: Only one auction view per app.
    let mutable private bidViewMap =
        System.Collections.Generic.Dictionary<Seat, BidView>(Seat.numSeats)

    /// Bid animation destinations.
    // To-do: Come up with a better way to position these.
    let private destMap =
        Map [
            Seat.West,  Pt (-0.83,  0.60)
            Seat.North, Pt (-0.31, -0.90)
            Seat.East,  Pt ( 0.82,  0.60)
            Seat.South, Pt (-0.31,  1.27)
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
        bidViewMap.[seat] <- bidView

            // animate the the bid view to its destination
        let dest =
            surface |> CardSurface.getPosition destMap.[seat]
        [|
            AnimationAction.moveTo dest
                |> ElementAction.create bidView
                |> Animation.Unit
            Animation.Sleep 1000
        |] |> Animation.Serial

    /// Animates the removal of the current bid views.
    let removeAnim () =

            // create animation
        let anim =
            bidViewMap
                |> Seq.map (fun (KeyValue(_, bidView)) ->
                    AnimationAction.Remove
                        |> ElementAction.create bidView
                        |> Animation.Unit)
                |> Seq.toArray
                |> Animation.Parallel

            // reset to new auction
        bidViewMap.Clear()

        anim

    /// Animates establishing trump.
    let establishTrumpAnim seat trump =

            // get existing bid view
        let oldBidView = bidViewMap.[seat]
        let bid = oldBidView |> BidView.bid

            // create new bid view
        let newBidView = BidView.createTrump bid trump
        newBidView.css
            {|
                position = "absolute"
            |}
        bidViewMap.[seat] <- newBidView

            // create animation
        AnimationAction.ReplaceWith newBidView
            |> ElementAction.create oldBidView
            |> Animation.Unit
