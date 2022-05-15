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

    // Coords of the center of each hand.
    let private handCoordsMap =
        Map [
            Seat.West,  (-0.7,  0.0)
            Seat.North, ( 0.0, -0.9)
            Seat.East,  ( 0.7,  0.0)
            Seat.South, ( 0.0,  0.9)
        ]

    /// Deals the cards in the given hand view into their target
    /// position.
    let private deal surface seat (handView : HandView) =

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
                        let coords = handCoordsMap.[seat]
                        let iCard' = iCard + cardOffset
                        seq {
                            animateCard surface coords numCards iCard'
                            BringToFront
                        }
                    for action in actions do
                        yield Animation.create cardView action
            } |> Animation.Parallel

        animate 0 batch1, animate batchSize batch2

    // Deals the given cards to each hand.
    let dealW surface = deal surface Seat.West
    let dealN surface = deal surface Seat.North
    let dealE surface = deal surface Seat.East
    let dealS surface = deal surface Seat.South

    /// Animates adjustment of remaining unplayed cards in a hand.
    let adjust surface seat (handView : HandView) =
        let numCards = handView.Length
        handView
            |> Seq.mapi (fun iCard cardView ->
                let coords = handCoordsMap.[seat]
                animateCard surface coords numCards iCard
                    |> Animation.create cardView)
            |> Animation.Parallel

    let playCoordsMap =
        Map [
            Seat.West,  (-0.2,  0.0)
            Seat.North, ( 0.0, -0.3)
            Seat.East,  ( 0.2,  0.0)
            Seat.South, ( 0.0,  0.3)
        ]

module ClosedHandView =

    let ofCardViews (cardViews : seq<CardView>) : HandView =
        cardViews
            |> Seq.toArray

    /// Answers a function that can be called to animate the playing
    /// of a card from the given hand view
    let private play surface seat (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove arbitrary card from hand
            let back = cardViewsMut |> Seq.last
            let flag = cardViewsMut.Remove(back)
            assert(flag)

                // animate card being played
            let animPlay =
                let coords = HandView.playCoordsMap.[seat]
                seq {
                    ReplaceWith cardView
                        |> Animation.create back
                    surface
                        |> CardSurface.getPosition coords
                        |> MoveTo
                        |> Animation.create cardView
                } |> Animation.Serial

                // animate adjustment of remaining cards to fill gap
            let animAdjust =
                cardViewsMut
                    |> Seq.toArray
                    |> HandView.adjust surface seat

                // run animations in parallel
            seq {
                animPlay
                animAdjust
            } |> Animation.Parallel

    let playW surface = play surface Seat.West
    let playN surface = play surface Seat.North
    let playE surface = play surface Seat.East
    let playS surface = play surface Seat.South

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

    /// Answers a function that can be called to animate the playing
    /// of a card from the given open hand view
    let private play surface seat (handView : HandView) =
        let mutable cardViewsMut = ResizeArray(handView)
        fun (cardView : CardView) ->

                // remove selected card from hand
            let flag = cardViewsMut.Remove(cardView)
            assert(flag)

                // animate card being played
            let animPlay =
                let coords = HandView.playCoordsMap.[seat]
                seq {
                    BringToFront
                    surface
                        |> CardSurface.getPosition coords
                        |> MoveTo
                }
                    |> Seq.map (Animation.create cardView)
                    |> Animation.Serial

                // animate adjustment of remaining cards to fill gap
            let animAdjust =
                cardViewsMut
                    |> Seq.toArray
                    |> HandView.adjust surface seat

                // run animations in parallel
            seq {
                animPlay
                animAdjust
            } |> Animation.Parallel

    let playW surface = play surface Seat.West
    let playN surface = play surface Seat.North
    let playE surface = play surface Seat.East
    let playS surface = play surface Seat.South
