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

module HandView =

    /// Target distance between adjacent cards in the hand.
    let delta : Coord = 0.05

    /// Gets the target x-coord of the given card in a hand containing
    /// the given total number of cards.
    let getX numCards =

        /// Left-shift from center of hand.
        let shift = 0.5 * float (numCards - 1) * delta

        fun iCard ->
            (delta * float iCard) - shift

    /// Animates the given card view to its target position.
    let animateCard (xOffset : Coord, y : Coord) surface iCard cardView : AnimationStep =
        let pos =
            let x = getX 6 iCard + xOffset
            CardSurface.getPosition (x, y) surface
        [
            MoveTo pos
            BringToFront
        ] |> Seq.map (ElementAction.create cardView)

    /// Deals the given batches of cards into their target positions.
    let deal coords surface cardViews1 cardViews2 =

        let batchSize = Setback.numCardsPerHand / 2
        assert(cardViews1 |> Seq.length = batchSize)
        assert(cardViews2 |> Seq.length = batchSize)

        let gen cardOffset =
            Seq.mapi (fun iCard ->
                let iCard' = iCard + cardOffset
                animateCard coords surface iCard')
                >> Seq.concat
        gen 0 cardViews1, gen batchSize cardViews2

    let west = deal (-0.7, 0.0)
    let north = deal (0.0, -0.9)
    let east = deal (0.7, 0.0)
    let south = deal (0.0, 0.9)

module DealView =

    let create surface deal : Animation =

        let backs =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init 52 (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

        let stepW1, stepW2 = HandView.west surface backs.[0..2] backs.[12..14]
        let stepN1, stepN2 = HandView.north surface backs.[3..5] backs.[15..17]
        let stepE1, stepE2 = HandView.east surface backs.[6..8] backs.[18..20]
        let stepS1, stepS2 = HandView.south surface backs.[9..11] backs.[21..23]

        let reveal =
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
            for cv in hand do
                cv.click (fun () -> cv.remove())
            [
                let pairs =
                    Seq.append
                        (Seq.zip backs.[9..11] hand.[0..2])
                        (Seq.zip backs.[21..23] hand.[3..5])
                for back, front in pairs do
                    yield ElementAction.create
                        back (ReplaceWith front)
                for back in backs.[24..] do
                    yield ElementAction.create
                        back Remove
            ]

        [
            stepW1; stepN1; stepE1; stepS1
            stepW2; stepN2; stepE2; stepS2
            reveal
        ]

module App =

    let run deal =
        let surface = CardSurface.init "#surface"
        DealView.create surface deal
            |> Animation.run

    (~~document).ready (fun () ->

        let rng = Random()
        let dealer = Seat.South
        let deal =
            Deck.shuffle rng
                |> AbstractOpenDeal.fromDeck dealer

        run deal)
