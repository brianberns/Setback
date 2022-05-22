namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let private auction surface dealer deal =
        let chooseBid (handler : Bid -> unit) (bids : Set<Bid>) =
            let chooser = BidChooser.create bids handler
            surface.Element.append(chooser)
        Auction.run dealer deal chooseBid

    let private playout surface dealer deal handViews =
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
        Playout.play dealer deal playoutMap

    let private run () =

        let surface = CardSurface.init "#surface"

        let rng = Random()
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        promise {
            let! seatViews = DealView.start surface dealer deal
            auction surface dealer deal (fun deal' ->
                if deal'.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                    for (_, handView) in seatViews do
                        for cardView in handView do
                            cardView.remove()
                else
                    playout surface dealer deal' seatViews)
        } |> ignore

        // start the game when the browser is ready
    (~~document).ready(run)
