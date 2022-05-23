namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module Deal =

    /// Runs the auction of the given deal.
    let private auction surface dealer score deal =

        /// Creates bid chooser.
        let chooseBid handler legalBids =
            let chooser = BidChooser.create legalBids handler
            surface.Element.append(chooser)

            // get bid animation for each seat
        let auctionMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let animBid =
                        AuctionView.bidAnim surface seat
                    seat, animBid)
                |> Map

            // run the auction
        Auction.run dealer score deal chooseBid auctionMap

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

    /// Runs the given deal.
    let run surface dealer score deal seatViews cont =
        auction surface dealer score deal (fun deal' ->

            if deal'.ClosedDeal.Auction.HighBid.Bid = Bid.Pass then
                for (_, handView) in seatViews do
                    for (cardView : CardView) in handView do
                        cardView.remove()
                cont deal'

            else
                playout surface dealer deal' seatViews cont)

module Game =

    let run surface rng dealer =

        let rec loop (game : Game) dealer =
            promise {

                    // create random deal
                console.log($"Dealer is {Seat.toString dealer}")
                let deal =
                    Deck.shuffle rng
                        |> AbstractOpenDeal.fromDeck dealer

                    // animate dealing the cards
                let! seatViews = DealView.start surface dealer deal

                    // run the deal
                Deal.run
                    surface
                    dealer
                    game.Score
                    deal
                    seatViews
                    (fun deal' ->

                        let dealScore =
                            deal' |> AbstractOpenDeal.dealScore
                        let dealer' = dealer.Next
                        do
                            let absScore = Game.shiftScore dealer dealScore
                            console.log($"E+W make {absScore.[0]} point(s)")
                            console.log($"N+S make {absScore.[1]} point(s)")

                        let gameScore = game.Score + dealScore
                        do
                            let absScore = Game.shiftScore dealer gameScore
                            console.log($"E+W now have {absScore.[0]} point(s)")
                            console.log($"N+S now have {absScore.[1]} point(s)")

                        let game' =
                            let score'' = gameScore |> AbstractScore.shift 1
                            { game with Score = score'' }
                        loop game' dealer')
            } |> ignore

        loop Game.zero dealer

module App =

    let private run () =
        let surface = CardSurface.init "#surface"
        let rng = Random()
        Game.run surface rng Seat.South

        // start the game when the browser is ready
    (~~document).ready(run)
