namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

type HandView = CardView[]

module HandView =

    /// Gets the target x-coord of the given card in a hand
    /// containing the given total number of cards.
    let private getX numCards iCard : Coord =

        /// Target distance between adjacent cards in the hand.
        let delta : Coord = 0.05

        /// Left-shift from center of hand.
        let shift = 0.5 * float (numCards - 1) * delta

        (delta * float iCard) - shift

    /// Animates a card to its target position in a hand.
    let private animateCard surface (xOffset : Coord, y : Coord)
        numCards iCard =
        let x = getX numCards iCard + xOffset
        surface
            |> CardSurface.getPosition (x, y)
            |> MoveTo

    /// Deals the cards in the given hand view into their target
    /// position.
    let private deal surface coords (handView : HandView) =

        assert(handView.Length = Setback.numCardsPerHand)
        let batchSize = Setback.numCardsPerHand / 2
        let batch1 = handView.[..batchSize-1]
        let batch2 = handView.[batchSize..]

        /// Animate the given batch of cards.
        let animate cardOffset (cardViews : CardView[]) =
            let numCards = Setback.numCardsPerHand
            seq {
                for iCard = 0 to batchSize - 1 do
                    let cardView = cardViews.[iCard]
                    let actions =
                        let iCard' = iCard + cardOffset
                        seq {
                            animateCard surface coords numCards iCard'
                            BringToFront
                        }
                    for action in actions do
                        yield Animation.create cardView action
            } |> Animation.Parallel

        animate 0 batch1, animate batchSize batch2

    // Coords of the center of each hand.
    let private coordsW = (-0.7,  0.0)
    let private coordsN = ( 0.0, -0.9)
    let private coordsE = ( 0.7,  0.0)
    let private coordsS = ( 0.0,  0.9)

    // Deals the given cards to each hand.
    let dealW surface = deal surface coordsW
    let dealN surface = deal surface coordsN
    let dealE surface = deal surface coordsE
    let dealS surface = deal surface coordsS

    /// Answers a function that can be called to animate the playing
    /// of a card from the given hand view
    let private play surface coords (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove selected card from hand
            let flag = cardViewsMut.Remove(cardView)
            assert(flag)

                // animate card being played
            let animPlay =
                seq {
                    BringToFront
                    surface
                        |> CardSurface.getPosition coords
                        |> MoveTo
                }
                    |> Seq.map (Animation.create cardView)
                    |> Animation.Parallel

                // animate adjustment of remaining cards to fill gap
            let animRemain =
                let numCards = cardViewsMut.Count
                cardViewsMut
                    |> Seq.mapi (fun iCard cardView ->
                        animateCard surface coordsS numCards iCard
                            |> Animation.create cardView)
                    |> Animation.Parallel

                // run animations in parallel
            seq {
                animPlay
                animRemain
            } |> Animation.Parallel

    let playW surface = play surface (-0.3,  0.0)
    let playN surface = play surface ( 0.0, -0.3)
    let playE surface = play surface ( 0.3,  0.0)
    let playS surface = play surface ( 0.0,  0.3)

module ClosedHandView =

    let ofCardViews (cardViews : seq<CardView>) : HandView =
        cardViews
            |> Seq.toArray

module OpenHandView =

    /// Creates an open view of the given hand.
    let create (hand : Hand) : HandView =
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

    /// Reveals the given open hand view.
    let reveal (closedHandView : HandView) (openHandView : HandView) =
        assert(closedHandView.Length = openHandView.Length)
        (closedHandView, openHandView)
            ||> Seq.map2 (fun back front ->
                Animation.create back (ReplaceWith front))
            |> Animation.Parallel
