namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

type HandView = CardView[]

module HandView =

    /// Gets the target x-coord of the given card in a hand
    /// containing the given total number of cards.
    let private getX =

        /// Target distance between adjacent cards in the hand.
        let delta : Coord = 0.05

        fun numCards ->

            /// Left-shift from center of hand.
            let shift = 0.5 * float (numCards - 1) * delta

            fun iCard ->
                (delta * float iCard) - shift

    /// Animates a card to its target position in a hand.
    let private animateCard
        (xOffset : Coord, y : Coord)
        surface numCards iCard =
        let x = getX numCards iCard + xOffset
        CardSurface.getPosition (x, y) surface
            |> MoveTo

    /// Deals the given batches of cards into their target positions.
    let private deal coords surface cardViews1 cardViews2 =

        let batchSize = Setback.numCardsPerHand / 2
        assert(cardViews1 |> Seq.length = batchSize)
        assert(cardViews2 |> Seq.length = batchSize)

        let gen cardOffset (cardViews : _[]) =
            let numCards = Setback.numCardsPerHand
            seq {
                for iCard = 0 to batchSize - 1 do
                    let cardView = cardViews.[iCard]
                    let actions =
                        let iCard' = iCard + cardOffset
                        seq {
                            animateCard coords surface numCards iCard'
                            BringToFront
                        }
                    for action in actions do
                        yield Animation.create cardView action
            } |> Animation.Parallel
        gen 0 cardViews1, gen batchSize cardViews2

    let private coordsWest  = -0.7,  0.0
    let private coordsNorth =  0.0, -0.9
    let private coordsEast  =  0.7,  0.0
    let private coordsSouth =  0.0,  0.9

    let dealWest  surface cvs1 cvs2 = deal coordsWest  surface cvs1 cvs2
    let dealNorth surface cvs1 cvs2 = deal coordsNorth surface cvs1 cvs2
    let dealEast  surface cvs1 cvs2 = deal coordsEast  surface cvs1 cvs2
    let dealSouth surface cvs1 cvs2 = deal coordsSouth surface cvs1 cvs2

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

    let reveal cardBacks (handView : HandView) =
        assert(cardBacks |> Seq.length = handView.Length)
        seq {
            let pairs =
                Seq.zip cardBacks handView
            for (back : CardView), front in pairs do
                yield Animation.create
                    back (ReplaceWith front)
        } |> Animation.Parallel

    /// Answers a function that can be called to animate the playing of
    /// a card.
    let play surface (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove selected card from hand
            let flag = cardViewsMut.Remove(cardView)
            assert(flag)

                // animate card being played
            let animPlay =
                seq {
                    BringToFront
                    CardSurface.getPosition (0.0, 0.3) surface
                        |> MoveTo
                }
                    |> Seq.map (Animation.create cardView)
                    |> Animation.Serial

                // animate adjustment of remaining cards to fill gap
            let animRemain =
                let numCards = cardViewsMut.Count
                cardViewsMut
                    |> Seq.mapi (fun iCard cardView ->
                        animateCard coordsSouth surface numCards iCard
                            |> Animation.create cardView)
                    |> Animation.Parallel

                // run animations in parallel
            seq {
                animPlay
                animRemain
            } |> Animation.Parallel
