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

    /// All bids.
    let allBids = Enum.getValues<Bid>

    /// Total number of bids.
    let numBids = allBids.Length

#if FABLE_COMPILER
    /// Display name.
    let toString = function
        | Bid.Pass -> "Pass"
        | Bid.Two -> "Two"
        | Bid.Three -> "Three"
        | Bid.Four -> "Four"
        | _ -> failwith "Unexpected bid"

    let fromString = function
        | "Pass" -> Bid.Pass
        | "Two" -> Bid.Two
        | "Three" -> Bid.Three
        | "Four" -> Bid.Four
        | _ -> failwith "Unexpected bid"
#endif
