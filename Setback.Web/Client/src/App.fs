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

type CardSurface =
    {
        Element : JQueryElement
        Width : Length
        Height : Length
    }

module CardSurface =

    let private border = Pixel 1.0

    let init selector =
        let elem = JQuery.select selector
        {
            Element = elem
            Width =
                let width = Length.ofElement "width" elem
                width - CardView.width - (2.0 * border)

            Height =
                let height = Length.ofElement "height" elem
                height - CardView.height - (2.0 * border)
        }

    let getPosition x y surface =
        let left = Coord.toLength surface.Width x
        let top = Coord.toLength surface.Height y
        Position.create left top

module App =

    let run deal =

        let surface = CardSurface.init "#surface"

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
                        let pos = surface |> CardSurface.getPosition x 0.9
                        cardView |> CardView.animateTo pos
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
            let pos = surface |> CardSurface.getPosition 0.0 0.0
            cardView |> CardView.setPosition pos
            surface.Element.append(cardView)
        animateDeal stack []

    let rng = Random(0)
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    JQuery.init ()

    (~~document).ready (fun () ->
        run deal)
