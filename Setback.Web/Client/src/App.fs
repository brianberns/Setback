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

    /// Gets the target x-coord of the given card in a hand
    /// containing the given total number of cards.
    let getX numCards =

        /// Left-shift from center of hand.
        let shift = 0.5 * float (numCards - 1) * delta

        fun iCard ->
            (delta * float iCard) - shift

    /// Animates the given card view to its target position.
    let animateCard (xOffset : Coord, y : Coord) surface numCards iCard cardView : AnimationStep =
        let pos =
            let x = getX numCards iCard + xOffset
            CardSurface.getPosition (x, y) surface
        [
            MoveTo pos
            BringToFront
        ] |> Seq.map (ElementAction.create cardView)

    /// Deals the given batches of cards into their target positions.
    let deal coords surface cardViews1 cardViews2 : AnimationStep * AnimationStep =

        let batchSize = Setback.numCardsPerHand / 2
        assert(cardViews1 |> Seq.length = batchSize)
        assert(cardViews2 |> Seq.length = batchSize)

        let gen cardOffset =
            let numCards = Setback.numCardsPerHand
            Seq.mapi (fun iCard ->
                let iCard' = iCard + cardOffset
                animateCard coords surface numCards iCard')
                >> Seq.concat
        gen 0 cardViews1, gen batchSize cardViews2

    let private coordsWest  = -0.7,  0.0
    let private coordsNorth =  0.0, -0.9
    let private coordsEast  =  0.7,  0.0
    let private coordsSouth =  0.0,  0.9

    let dealWest  = deal coordsWest
    let dealNorth = deal coordsNorth
    let dealEast  = deal coordsEast
    let dealSouth = deal coordsSouth

    let private playSouth surface cardViews =
        let mutable cardViews' = ResizeArray (cardViews : seq<_>)
        for (cardView : CardView) in cardViews' do
            cardView.click (fun () ->

                    // remove selected card
                cardView.remove()
                let flag = cardViews'.Remove(cardView)
                assert(flag)

                    // adjust remaining cards to fill gap
                let numCards = cardViews'.Count
                cardViews'
                    |> Seq.mapi (fun iCard cv ->
                        animateCard coordsSouth surface numCards iCard cv)
                    |> Seq.concat
                    |> Seq.singleton
                    |> Animation.run)

    let revealSouth surface cardBacks (hand : Hand) =
        let cardViews =
            hand
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
        playSouth surface cardViews
        seq {
            let pairs =
                Seq.zip cardBacks cardViews
            for (back : CardView), front in pairs do
                yield ElementAction.create
                    back (ReplaceWith front)
        }

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

        let stepW1, stepW2 = HandView.dealWest  surface backs.[0.. 2] backs.[12..14]
        let stepN1, stepN2 = HandView.dealNorth surface backs.[3.. 5] backs.[15..17]
        let stepE1, stepE2 = HandView.dealEast  surface backs.[6.. 8] backs.[18..20]
        let stepS1, stepS2 = HandView.dealSouth surface backs.[9..11] backs.[21..23]

        let finish : AnimationStep =
            let southBacks =
                Seq.append backs.[9..11] backs.[21..23]
            let reveal =
                HandView.revealSouth
                    surface
                    southBacks
                    deal.UnplayedCards.[0]
            let remove =
                seq {
                    for back in backs.[24..] do
                        yield ElementAction.create
                            back Remove
                }
            Seq.append reveal remove

        [
            stepW1; stepN1; stepE1; stepS1
            stepW2; stepN2; stepE2; stepS2
            finish
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
