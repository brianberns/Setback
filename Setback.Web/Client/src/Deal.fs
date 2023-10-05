namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

module Deal =

    /// Runs the auction of the given deal.
    let private auction (surface : JQueryElement) dealer score deal =

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

            // run the auction
        Auction.run dealer score deal chooser auctionMap

    /// Runs the playout of the given deal.
    let private playout dealer deal handViews =

        /// Establishes trump.
        let establishTrumpAnim seat trump =
            for (seat : Seat, handView) in handViews do
                if seat.IsUser then
                    handView |> OpenHandView.establishTrump trump
            AuctionView.establishTrumpAnim seat trump

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
        Playout.run dealer deal playoutMap

    /// Handles the end of a deal.
    let private dealOver (surface : JQueryElement) dealer deal =

            // determine deal outcome
        let dealScore =
            deal
                |> AbstractOpenDeal.dealScore
                |> Game.absoluteScore dealer
        let highBid = deal.ClosedDeal.Auction.HighBid
        let bidder =
            assert(highBid.BidderIndex >= 0)
            dealer |> Seat.incr highBid.BidderIndex
        let bid = highBid.Bid

            // display banner
        let banner =
            let html =
                $"<p>{Seat.toString bidder} bid {Bid.toString bid}</p><p>East + West make {dealScore[0]}<br />North + South make {dealScore[1]}</p>"   // to-do: use team names from Game module
            ~~HTMLDivElement.Create(innerHTML = html)
        banner.addClass("banner")
        surface.append(banner)

            // wait for user to click banner
        Promise.create (fun resolve _reject ->
            banner.click(fun () ->
                banner.remove()
                resolve ()))

    /// Runs one deal.
    let run surface persState score =
        async {

                // create random deal
            let rng = Random(persState.RandomState)
            let dealer = persState.Dealer
            do
                console.log($"Deal #{string rng.State}")
                console.log($"Dealer is {Seat.toString dealer}")
            let deal =
                persState.DealOpt
                    |> Option.defaultWith (fun () ->
                            Deck.shuffle rng
                                |> AbstractOpenDeal.fromDeck dealer)

                // reset game points won
            DealView.displayStatus dealer deal

                // animate dealing the cards
            let! seatViews =
                DealView.start surface dealer deal
                    |> Async.AwaitPromise

                // run the auction
            let! postAuctionDeal =
                auction surface dealer score deal

            /// Completes the given deal.
            let complete completedDeal =
                {
                    persState with
                        RandomState = rng.State
                        DealOpt = Some completedDeal
                }

                // force cleanup after all-pass auction
            if postAuctionDeal.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                for (_, handView) in seatViews do
                    for (cardView : CardView) in handView do
                        cardView.remove()
                return complete postAuctionDeal

                // run the playout
            else
                let! postPlayoutDeal = playout dealer postAuctionDeal seatViews
                do! dealOver surface dealer postPlayoutDeal
                    |> Async.AwaitPromise
                return complete postPlayoutDeal
        }
