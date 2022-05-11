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

type CardSurface =
    {
        Element : JQueryElement
        Width : Length
        Height : Length
    }

module CardSurface =

    let init selector =
        let elem = JQuery.select selector
        {
            Element = elem
            Width =
                let width = JQueryElement.length "width" elem
                width - CardView.width - (2.0 * CardView.border)
            Height =
                let height = JQueryElement.length "height" elem
                height - CardView.height - (2.0 * CardView.border)
        }

    let getPosition (x, y) surface =
        let left = Coord.toLength surface.Width x
        let top = Coord.toLength surface.Height y
        Position.create left top

module DealView =

    let incr = 0.1

    let getCoord i =
        (incr * float i) - (0.5 * (6.0 - 1.0) * incr)

    let create surface deal : Animation =

        let createActions (x : Coord, y : Coord) cardOffset iCard cv =
            let pos =
                CardSurface.getPosition
                    (getCoord (iCard + cardOffset) + x, y)
                    surface
            [
                MoveTo pos
                BringToFront
            ] |> Seq.map (ElementAction.create cv)

        let generate coords cvs1 cvs2 : AnimationStep * AnimationStep =
            let gen cardOffset =
                Seq.mapi (createActions coords cardOffset)
                    >> Seq.concat
            gen 0 cvs1, gen 3 cvs2

        let west = generate (-0.7, 0.0)
        let north = generate (0.0, -0.9)
        let east = generate (0.7, 0.0)
        let south = generate (0.0, 0.9)

        let backs =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init 52 (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

        let stepW1, stepW2 = west backs.[0..2] backs.[12..14]
        let stepN1, stepN2 = north backs.[3..5] backs.[15..17]
        let stepE1, stepE2 = east backs.[6..8] backs.[18..20]
        let stepS1, stepS2 = south backs.[9..11] backs.[21..23]

        let hand =
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
                |> Seq.map CardView.ofCard
                |> Seq.toArray

        let replace =
            [
                ElementAction.create
                    backs.[9]
                    (ReplaceWith hand.[0])
                ElementAction.create
                    backs.[10]
                    (ReplaceWith hand.[1])
                ElementAction.create
                    backs.[11]
                    (ReplaceWith hand.[2])

                ElementAction.create
                    backs.[21]
                    (ReplaceWith hand.[3])
                ElementAction.create
                    backs.[22]
                    (ReplaceWith hand.[4])
                ElementAction.create
                    backs.[23]
                    (ReplaceWith hand.[5])
            ]

        [
            stepW1; stepN1; stepE1; stepS1
            stepW2; stepN2; stepE2; stepS2
            replace
        ]

module App =

    let run deal =
        let surface = CardSurface.init "#surface"
        DealView.create surface deal
            |> Animation.run

    (~~document).ready (fun () ->

        let rng = Random(0)
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        run deal)
