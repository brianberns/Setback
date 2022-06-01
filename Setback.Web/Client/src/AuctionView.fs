namespace Setback.Web.Client

open Setback

/// View of a each seat's bid in an auction.
module AuctionView =

    /// Bids made in this auction.
    /// ASSUMPTION: Only one auction view per app.
    let mutable private bidViewMap =
        System.Collections.Generic.Dictionary<Seat, BidView>(Seat.numSeats)

    /// Bid animation destinations.
    let private destMap =
        Position.seatMap [
            Seat.West,  ( 2, 62)
            Seat.North, (30,  1)
            Seat.East,  (92, 62)
            Seat.South, (30, 94)
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
        JQueryElement.setPosition (Position.ofInts (50, 50)) bidView
        surface.Element.append(bidView)
        bidViewMap.[seat] <- bidView

            // animate the the bid view to its destination
        let dest = destMap.[seat]
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
