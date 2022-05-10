namespace Setback.Web.Client

open System

open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

/// Values from -1.0 to 1.0.
type Coord = float

module Coord =

    let toLength (max : Length) (coord : Coord) =
        (0.5 * (float max.NumPixels * (coord + 1.0)))
            |> Pixel

module App =

    let run deal =

        let surface = JQuery.select "#surface"
        let border = Pixel 1.0
        let maxWidth =
            let width =
                surface.css "width"
                    |> Length.parse
            width - CardView.width - (2.0 * border)
        let maxHeight =
            let height =
                surface.css "height"
                    |> Length.parse
            height - CardView.height - (2.0 * border)

        let setPosition x y cardView =
            let left = Coord.toLength maxWidth x
            let top = Coord.toLength maxHeight y
            cardView |> CardView.setPosition left top

        let animate x y cardView =
            let left = Coord.toLength maxWidth x
            let top = Coord.toLength maxHeight y
            cardView |> CardView.animateTo left top

        let rec animateDeal (undealt : List<CardView>) (dealt : List<CardView>) =

            let incr = 0.1
            for i = 0 to dealt.Length - 1 do
                let cardView = dealt.[i]
                let x = incr * (float i - 0.5 * float dealt.Length)
                cardView |> animate x 0.5

        let cardViews =
            deal.UnplayedCards.[0]
                |> Seq.sortByDescending (fun card ->
                    card.Suit, card.Rank)
                |> Seq.map CardView.create
                |> Seq.toList
        for cardView in cardViews do
            cardView |> setPosition 0.0 0.0
            surface.append(cardView)
        animateDeal [] cardViews

    let rng = Random()
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    JQuery.init ()

    (~~document).ready (fun () ->
        run deal)
