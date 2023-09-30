namespace Setback.Cfrm

#if !FABLE_COMPILER
open System
#endif

open Setback

/// Abstract view of the high bid in an auction.
type AbstractHighBid =
    {
        /// 0-based index of the current high bidder, relative to the
        /// dealer (or -1 if no current high bidder).
        BidderIndex : int

        /// High bid so far.
        Bid : Bid
    }

module AbstractHighBid =

    /// No high bid yet. This state persists until someone makes a bid
    /// other than Pass.
    let none =
        {
            BidderIndex = -1
            Bid = Bid.Pass
        }

    /// Creates a high bid for the given bidder, relative to the dealer.
    let create bidderIdx bid =
        assert(bidderIdx >= 0 && bidderIdx < Seat.numSeats)
        assert(bid <> Bid.Pass)
        {
            BidderIndex = bidderIdx
            Bid = bid
        }

    /// Applies setback penalty to the given deal score, if applicable.
    let finalizeDealScore (dealScore : AbstractScore) highBid =

            // did anyone actually bid?
        if highBid.Bid > Bid.Pass then

                // determine amount bid by auction-winning team
            let iBidderTeam =
                highBid.BidderIndex % Setback.numTeams
            let nBid = int highBid.Bid
            assert(nBid > 0)

                // apply penalty?
            if dealScore.[iBidderTeam] < nBid then
                AbstractScore [|
                    for iTeam = 0 to Setback.numTeams - 1 do
                        if iTeam = iBidderTeam then
                            yield -nBid
                        else
                            yield dealScore.[iTeam]
                |]
            else dealScore

        else
            assert(dealScore = AbstractScore.zero)
            dealScore

    /// String representation of a high bid.
    let layout =
        [|
            SpanLayout.ofLength 1   // bidder index
            SpanLayout.ofLength 1   // bid
        |] |> SpanLayout.combine

    /// String representation of a high bid.
    let copyTo (span : Span<_>) highBid =
        assert(span.Length = layout.Length)

            // bidder index
        let cIndex =
            if highBid.BidderIndex = -1 then '.'
            else Char.fromDigit highBid.BidderIndex
        layout.Slice(0, span).Fill(cIndex)

            // bid
        let cBid = highBid.Bid |> int |> Char.fromDigit
        layout.Slice(1, span).Fill(cBid)
