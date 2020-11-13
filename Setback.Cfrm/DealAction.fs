namespace Setback.Cfrm

/// Actions available in a deal.
type DealAction =

    /// Auction action.
    | DealBidAction of BidAction

    /// Playout action
    | DealPlayAction of PlayAction

module DealAction =

    /// String representation of a deal action.
    let layout =
        PlayAction.layout

    /// String representation of a deal action.
    let copyTo span = function
        | DealBidAction _ -> failwith "Unexpected"
        | DealPlayAction action -> PlayAction.copyTo span action
