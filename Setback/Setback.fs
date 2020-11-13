namespace Setback

module Setback =

    /// Number of cards dealt to each player.
    let numCardsPerHand = 6

    /// Number of cards in a deal.
    let numCardsPerDeal = numCardsPerHand * Seat.numSeats

    /// High, Low, Jack, and Game.
    let numDealPoints = 4

    /// Number of deal points required to win.
    let winThreshold = 11
