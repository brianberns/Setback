namespace Setback.Web.Client

open System

open Fable.Core.JS

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

type CardViewStack = List<CardView>

module CardViewStack =

    let ofCards cards =
        cards
            |> Seq.rev
            |> Seq.map CardView.ofCard
            |> Seq.rev
            |> Seq.toList

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
            match undealt with
                | [] -> ()
                | head :: undealt' ->
                    CardView.bringToFront head
                    let dealt' = head :: dealt
                    let numDealt = dealt'.Length
                    let incr = 0.1
                    for i = 0 to numDealt - 1 do
                        let cardView = dealt'.[i]
                        let x = incr * (float (numDealt - i - 1) - 0.5 * float (numDealt - 1))
                        cardView |> animate x 0.9
                    let callback () = animateDeal undealt' dealt'
                    setTimeout callback 500 |> ignore

        let stack =
            deal.UnplayedCards.[0]
                |> Seq.sortByDescending (fun card ->
                    let iSuit =
                        match card.Suit with
                            | Suit.Spades   -> 4   // black
                            | Suit.Hearts   -> 3   // red
                            | Suit.Clubs    -> 2   // black
                            | Suit.Diamonds -> 1   // red
                            | _ -> failwith "Unexpected"
                    iSuit, card.Rank)
                |> CardViewStack.ofCards
        for cardView in stack do
            cardView |> setPosition 0.0 0.0
            surface.append(cardView)
        animateDeal stack []

    let rng = Random(0)
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    JQuery.init ()

    (~~document).ready (fun () ->
        run deal)
