namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm
open Setback.Web.Client   // ugly - force AutoOpen

module Deal =

    /// Runs the auction of the given deal.
    let private auction (surface : JQueryElement) persState score =

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
        Auction.run persState score chooser auctionMap

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

                // new deal needed?
            let dealer = persState.Dealer
            let deal, persState =
                match persState.DealOpt with

                        // use existing deal
                    | Some deal -> deal, persState

                        // create random deal
                    | None ->
                        let rng = Random(persState.RandomState)
                        do
                            console.log($"Deal #{string rng.State}")
                            console.log($"Dealer is {Seat.toString dealer}")
                        let deal =
                            Deck.shuffle rng
                                |> AbstractOpenDeal.fromDeck dealer
                        let persState =
                            { persState with
                                RandomState = rng.State
                                DealOpt = Some deal }.Save()
                        deal, persState

                // reset game points won
            DealView.displayStatus dealer deal

                // animate dealing the cards
            let! seatViews =
                DealView.start surface dealer deal
                    |> Async.AwaitPromise

                // run the auction
            let! persState = auction surface persState score

                // force cleanup after all-pass auction
            if persState.Deal.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                for (_, handView) in seatViews do
                    for (cardView : CardView) in handView do
                        cardView.remove()
                return persState

                // run the playout
            else
                let! postPlayoutDeal = playout dealer persState.Deal seatViews
                do! dealOver surface dealer postPlayoutDeal
                    |> Async.AwaitPromise
                return { persState with DealOpt = Some postPlayoutDeal }
        }
