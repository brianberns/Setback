namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

/// Represents a (mutable) hand of cards.
type HandView = ResizeArray<CardView>

module HandView =

    /// Gets the target left coord of the given card in a hand
    /// containing the given total number of cards.
    let private getLeft numCards iCard =

        /// Target distance between adjacent cards in the hand.
        let delta = 2.5

        /// Left-shift from center of hand.
        let shift = 0.5 * float (numCards - 1) * delta

        (delta * float iCard) - shift
            |> Percent

    /// Gets the position of a card in a hand.
    let private getPosition centerPos numCards iCard =
        { centerPos with
            left = getLeft numCards iCard + centerPos.left }

    /// Animates a card to its target position in a hand.
    let private animateCard centerPos numCards iCard =
        getPosition centerPos numCards iCard
            |> AnimationAction.moveTo

    /// Center position of each hand.
    let private centerPosMap =
        Position.seatMap [
            Seat.West,  (12, 50)
            Seat.North, (50, 16)
            Seat.East,  (88, 50)
            Seat.South, (50, 83)
        ]

    /// Deals the cards in the given hand view into their target
    /// position.
    let dealAnim seat (handView : HandView) =

        assert(handView.Count = Setback.numCardsPerHand)
        let batchSize = Setback.numCardsPerHand / 2
        let batch1, batch2 =
            let cardViews = Seq.toArray handView
            cardViews[..batchSize-1],   // 0, 1, 2
            cardViews[batchSize..]      // 3, 4, 5

        /// Animate the given batch of cards.
        let animate cardOffset (cardViews : CardView[]) =
            let numCards = Setback.numCardsPerHand
            [|
                for iCard = 0 to batchSize - 1 do
                    let cardView = cardViews[iCard]
                    let actions =
                        let centerPos = centerPosMap[seat]
                        let iCard' = iCard + cardOffset
                        seq {
                            animateCard centerPos numCards iCard'
                            BringToFront
                        }
                    for action in actions do
                        yield Animation.create cardView action
            |] |> Animation.Parallel

        animate 0 batch1, animate batchSize batch2

    /// Sets the positions of the cards in the given hand, without
    /// animation.
    let setPositions seat (handView : HandView) =
        for iCard = 0 to handView.Count - 1 do
            let cardView = handView[iCard]
            let pos =
                let centerPos = centerPosMap[seat]
                getPosition centerPos handView.Count iCard
            JQueryElement.setPosition pos cardView

    /// Animates adjustment of remaining unplayed cards in a hand.
    let adjustAnim seat (handView : HandView) =
        let numCards = handView.Count
        handView
            |> Seq.mapi (fun iCard cardView ->
                let centerPos = centerPosMap[seat]
                animateCard centerPos numCards iCard
                    |> Animation.create cardView)
            |> Seq.toArray
            |> Animation.Parallel

module ClosedHandView =

    /// Creates a closed view of the given cards.
    let ofCardViews (cardViews : seq<CardView>) : HandView =
        assert(cardViews |> Seq.forall CardView.isBack)
        ResizeArray(cardViews)

    /// Answers a function that can be called to animate the playing
    /// of a card from the given closed hand view.
    let playAnim seat (handView : HandView) =
        fun (cardView : CardView) ->

                // remove arbitrary card from hand
            let back = handView |> Seq.last
            assert(back |> CardView.isBack)
            let flag = handView.Remove(back)
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
                TrickView.playAnim seat cardView
            |]

module OpenHandView =

    /// Creates an open view of the given hand.
    let ofHand (hand : Hand) : Fable.Core.JS.Promise<HandView> =
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
            |> Seq.map (CardView.ofCard false)
            |> Promise.all
            |> Promise.map ResizeArray

    /// Reveals the given open hand view.
    let revealAnim (closedHandView : HandView) (openHandView : HandView) =
        assert(closedHandView.Count = openHandView.Count)
        (closedHandView, openHandView)
            ||> Seq.map2 (fun back front ->
                Animation.create back (ReplaceWith front))
            |> Seq.toArray
            |> Animation.Parallel

    /// Answers a function that can be called to animate the playing
    /// of a card from the given open hand view.
    let playAnim seat (handView : HandView) =
        fun (cardView : CardView) ->

                // remove selected card from hand
            let flag = handView.Remove(cardView)
            assert(flag)

                // animate card being played
            let animPlay =
                [|
                    BringToFront                       // bring card to front
                        |> Animation.create cardView
                    TrickView.playAnim seat cardView   // slide revealed card to center
                |] |> Animation.Serial

                // animate adjustment of remaining cards to fill gap
            let animAdjust = HandView.adjustAnim seat handView

                // animate in parallel
            [|
                animPlay
                animAdjust
            |] |> Animation.Parallel

    /// Establishes trump in the given hand view.
    let establishTrump trump (handView : HandView) =
        for cardView in handView do
            let card = cardView |> CardView.card
            if card.Suit = trump then
                cardView.addClass("trump")
