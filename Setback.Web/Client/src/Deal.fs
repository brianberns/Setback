namespace Setback.Web.Client

open Browser

open Fable.Core

open PlayingCards
open Setback
open Setback.Cfrm

module Deal =

    /// Runs the auction of the given deal.
    let private auction surface dealer score deal =

            // create bid chooser
        let chooser = BidChooser.create ()
        surface.Element.append(chooser.Element)

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
    let private playout surface dealer deal handViews =

            // get animations for each seat
        let playoutMap =
            handViews
                |> Seq.map (fun (seat : Seat, handView) ->

                    let animCardPlay =
                        let anim =
                            if seat.IsUser then OpenHandView.playAnim
                            else ClosedHandView.playAnim
                        anim surface seat handView

                    let animTrickFinish =
                        TrickView.finishAnim surface

                    seat, (handView, animCardPlay, animTrickFinish))
                |> Map

            // run the playout
        Playout.play dealer deal playoutMap

    /// Runs one new deal.
    let run surface rng dealer score =
        async {

                // create random deal
            console.log($"Dealer is {Seat.toString dealer}")
            let deal =
                Deck.shuffle rng
                    |> AbstractOpenDeal.fromDeck dealer

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
                return! playout surface dealer deal' seatViews
        }
