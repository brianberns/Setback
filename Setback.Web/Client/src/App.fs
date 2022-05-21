namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let private auction surface dealer deal =
        let chooseBid (handler : Bid -> unit) (bids : Set<Bid>) =
            let pos = surface |> CardSurface.getPosition Point.origin
            let chooser = BidViewChooser.create pos bids handler
            surface.Element.append(chooser)
        Auction.run dealer deal chooseBid

    let private playout surface dealer deal handViews =
        let playoutMap =
            handViews
                |> Seq.map (fun (seat : Seat, handView) ->

                    let animCardPlay =
                        let play =
                            if seat.IsUser then OpenHandView.play
                            else ClosedHandView.play
                        play surface seat handView

                    let animTrickFinish =
                        TrickView.finish surface

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
            let! handViews = DealView.start surface dealer deal
            auction surface dealer deal (fun deal' ->
                playout surface dealer deal' handViews)
        } |> ignore

        // start the game when the browser is ready
    (~~document).ready(run)
