namespace Setback

open PlayingCards

/// Number of points a team commits to taking during the auction.
/// Note: No "One" bid in Setback!
type Bid =
    | Pass = 0
    | Two = 2
    | Three = 3
    | Four = 4

module Bid =

    /// Total number of bids.
    let numBids =
        Enum.getValues<Bid>.Length
