namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback

module Deal =

    /// Runs the auction of the given deal.
    let private auction
        (surface : JQueryElement)
        (persState : PersistentState)
        trumpOpt =

            // create bid chooser
        let chooser = BidChooser.create ()
        surface.append(chooser.Element)

            // get bid animation for each seat
        let auctionMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let animBid =
                        AuctionView.bidAnim surface seat
                    seat, animBid)
                |> Map

            // create views for bids already made
        let playerBids =
            persState.Game.Deal.ClosedDeal.Auction
                |> Auction.playerBids
        for seat, bid in playerBids do
            if seat = Seat.User then
                chooser.Element.remove()   // won't need this
            AuctionView.createBidView surface seat bid trumpOpt
                |> ignore

            // run the auction
        Auction.run persState chooser auctionMap

    /// Runs the playout of the given deal.
    let private playout
        (surface : JQueryElement)
        persState
        handViews =

        /// Establishes trump.
        let establishTrumpAnim seat trump =
            for (seat : Seat, handView) in handViews do
                if seat.IsUser then
                    handView |> OpenHandView.establishTrump trump
            AuctionView.establishTrumpAnim seat trump

            // create play chooser
        let chooser = PlayChooser.create ()
        surface.append(chooser.Element)

            // get animations for each seat
        let playoutMap =
            handViews
                |> Seq.map (fun (seat, handView) ->

                    let animCardPlay =
                        let anim =
                            if seat.IsUser then OpenHandView.playAnim
                            else ClosedHandView.playAnim
                        anim seat handView

                    let tuple =
                        handView,
                        animCardPlay,
                        TrickView.finishAnim,
                        establishTrumpAnim

                    seat, tuple)
                |> Map

            // run the playout
        async {
            let! persState' = Playout.run persState chooser playoutMap
            chooser.Element.remove()
            return persState'
        }

    /// Handles the end of a deal.
    let private dealOver (surface : JQueryElement) dealer deal =

            // determine deal outcome
        let dealScore =
            let playout =
                match deal.ClosedDeal.PlayoutOpt with
                    | Some playout -> playout
                    | None -> failwith "Unexpected"
            Playout.getDealScore playout
        let highBid = deal.ClosedDeal.Auction.HighBid
        assert(highBid <> Bid.Pass)
        let highBidder =
            match deal.ClosedDeal.Auction.HighBidderOpt with
                | Some bidder -> bidder
                | None -> failwith "Unexpected"

            // display banner
        let banner =
            let html =
                $"<p>{Seat.toString highBidder} bid {Bid.toString highBid}</p><p>East + West make {dealScore[Team.EastWest]}<br />North + South make {dealScore[Team.NorthSouth]}</p>"
            ~~HTMLDivElement.Create(innerHTML = html)
        banner.addClass("banner")
        surface.append(banner)

            // wait for user to click banner
        Promise.create (fun resolve _reject ->
            banner.click(fun () ->
                banner.remove()
                resolve ()))

    /// Runs one deal.
    let run surface persState =
        async {
                // new deal starting?
            let deal = persState.Game.Deal
            let dealer = deal.ClosedDeal.Auction.Dealer
            if deal.ClosedDeal.Auction.Bids.IsEmpty then
                console.log($"Dealer is {Seat.toString dealer}")
            else
                console.log("Finishing deal in progress")

                // animate dealing the cards
            DealView.displayStatus dealer deal
            let! seatViews =
                DealView.start surface dealer deal
                    |> Async.AwaitPromise

                // run the auction
            let! persState =
                deal.ClosedDeal.TrumpOpt   // needed when auction is already complete
                    |> auction surface persState

                // force cleanup after all-pass auction
            if persState.Game.Deal.ClosedDeal.Auction.HighBid = Bid.Pass then
                for (_, handView) in seatViews do
                    for cardView in handView do
                        cardView.remove()
                return persState

                // run the playout
            else
                let! persState = playout surface persState seatViews
                do! dealOver surface dealer persState.Game.Deal
                    |> Async.AwaitPromise
                return persState
        }
