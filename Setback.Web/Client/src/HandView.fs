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
    let private animateCard surface (Pt (xCenter, y))
        numCards iCard =
        let x = getX numCards iCard + xCenter
        surface
            |> CardSurface.getPosition (Pt (x, y))
            |> AnimationAction.moveTo

    /// Center point of each hand.
    let private centerPointMap =
        Map [
            Seat.West,  Pt (-0.7,  0.0)
            Seat.North, Pt ( 0.0, -0.9)
            Seat.East,  Pt ( 0.7,  0.0)
            Seat.South, Pt ( 0.0,  0.9)
        ]

    /// Deals the cards in the given hand view into their target
    /// position.
    let dealAnim surface seat (handView : HandView) =

        assert(handView.Length = Setback.numCardsPerHand)
        let batchSize = Setback.numCardsPerHand / 2
        let batch1 = handView.[..batchSize-1]
        let batch2 = handView.[batchSize..]

        /// Animate the given batch of cards.
        let animate cardOffset (cardViews : CardView[]) =
            let numCards = Setback.numCardsPerHand
            [|
                for iCard = 0 to batchSize - 1 do
                    let cardView = cardViews.[iCard]
                    let actions =
                        let centerPt = centerPointMap.[seat]
                        let iCard' = iCard + cardOffset
                        seq {
                            animateCard surface centerPt numCards iCard'
                            BringToFront
                        }
                    for action in actions do
                        yield Animation.create cardView action
            |] |> Animation.Parallel

        animate 0 batch1, animate batchSize batch2

    /// Animates adjustment of remaining unplayed cards in a hand.
    let adjustAnim surface seat (handView : HandView) =
        let numCards = handView.Length
        handView
            |> Array.mapi (fun iCard cardView ->
                let centerPt = centerPointMap.[seat]
                animateCard surface centerPt numCards iCard
                    |> Animation.create cardView)
            |> Animation.Parallel

module ClosedHandView =

    /// Creates a closed view of the given cards.
    let ofCardViews (cardViews : seq<CardView>) : HandView =
        cardViews
            |> Seq.toArray

    /// Answers a function that can be called to animate the playing
    /// of a card from the given closed hand view.
    let playAnim surface seat (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove arbitrary card from hand
            let back = cardViewsMut |> Seq.last
            let flag = cardViewsMut.Remove(back)
            assert(flag)

                // animate card being played
            Animation.Serial [|

                    // bring card to front
                BringToFront
                    |> Animation.create back

                    // reveal card
                ReplaceWith cardView
                    |> Animation.create back

                    // slide revealed card to center
                TrickView.playAnim surface seat cardView
            |]

module OpenHandView =

    /// Creates an open view of the given hand.
    let ofHand (hand : Hand) : HandView =
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
    let revealAnim (closedHandView : HandView) (openHandView : HandView) =
        assert(closedHandView.Length = openHandView.Length)
        (closedHandView, openHandView)
            ||> Array.map2 (fun back front ->
                Animation.create back (ReplaceWith front))
            |> Animation.Parallel

    /// Answers a function that can be called to animate the playing
    /// of a card from the given open hand view.
    let playAnim surface seat (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove selected card from hand
            let flag = cardViewsMut.Remove(cardView)
            assert(flag)

                // animate card being played
            let animPlay =
                [|
                    BringToFront                               // bring card to front
                        |> Animation.create cardView
                    TrickView.playAnim surface seat cardView   // slide revealed card to center
                |] |> Animation.Serial

                // animate adjustment of remaining cards to fill gap
            let animAdjust =
                cardViewsMut
                    |> Seq.toArray
                    |> HandView.adjustAnim surface seat

                // animate in parallel
            [|
                animPlay
                animAdjust
            |] |> Animation.Parallel
