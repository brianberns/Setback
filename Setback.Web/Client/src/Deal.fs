namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

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

            // get animations for each seat
        let playoutMap =
            handViews
                |> Seq.map (fun (seat : Seat, handView) ->

                    let animCardPlay =
                        let anim =
                            if seat.IsUser then OpenHandView.playAnim
                            else ClosedHandView.playAnim
                        anim seat handView

                    let tuple =
                        handView,
                        animCardPlay,
                        TrickView.finishAnim,
                        AuctionView.establishTrumpAnim

                    seat, tuple)
                |> Map

            // run the playout
        Playout.run dealer deal playoutMap

    /// Runs one new deal.
    let run surface rng dealer score =
        async {

                // create random deal
            console.log($"Dealer is {Seat.toString dealer}")
            let deal =
                Deck.shuffle rng
                    |> AbstractOpenDeal.fromDeck dealer

                // reset game points won
            DealView.displayStatus dealer deal

                // animate dealing the cards
            let! seatViews =
                DealView.start surface dealer deal
                    |> Async.AwaitPromise

                // run the auction and then playout
            let! deal' =
                auction surface dealer score deal

                // force cleanup after all-pass auction
            if deal'.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                for (_, handView) in seatViews do
                    for (cardView : CardView) in handView do
                        cardView.remove()
                return deal'

            else
                return! playout dealer deal' seatViews
        }
