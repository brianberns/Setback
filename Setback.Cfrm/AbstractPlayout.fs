namespace Setback.Cfrm

open System

open PlayingCards
open Setback

/// Abstract view of a playout used when playing cards in a deal.
type AbstractPlayout =
    {
        /// Winning high bid of auction.
        HighBid : AbstractHighBid

        /// Trump suit, if it's been established.
        TrumpOpt : Option<Suit>

        /// History of previous tricks.
        History : AbstractPlayoutHistory

        /// Current trick.
        CurrentTrick : AbstractTrick
    }

module AbstractPlayout =

    /// Initializes a playout with the given auction result.
    let create highBid =
        assert(highBid.BidderIndex >= 0)
        assert(highBid.Bid > Bid.Pass)
        {
            HighBid = highBid
            TrumpOpt = None
            History = AbstractPlayoutHistory.empty
            CurrentTrick =
                AbstractTrick.create None highBid.BidderIndex
        }

    /// Indicates whether the given playout is complete.
    let isComplete playout =
        playout.History
            |> AbstractPlayoutHistory.isComplete

    /// Index of the current player, relative to the dealer.
    let currentPlayerIndex (playout : AbstractPlayout) =
        assert(playout |> isComplete |> not)
        playout.CurrentTrick
            |> AbstractTrick.currentPlayerIndex

    /// What cards can be played now from the given hand?
    let legalPlays (hand : Hand) playout : seq<_> =
        assert(playout |> isComplete |> not)
        let trick = playout.CurrentTrick
        assert(trick.SuitLedOpt.IsNone = (trick.NumPlays = 0))
        match playout.TrumpOpt, trick.SuitLedOpt with

                // start trick with any card
            | _, None -> hand

                // continue trick by following suit, if possible, or trumping in
            | Some trump, Some suitLed ->
                let followsSuit (card : Card) =
                    card.Suit = suitLed
                if hand |> Seq.exists followsSuit then
                    hand |> Seq.where (fun card ->
                        card.Suit = trump || followsSuit card)
                else
                    hand

            | _ -> failwith "Unexpected"

    /// Adds the given card to the given playout.
    let addPlay (card : Card) playout =
        assert(playout |> isComplete |> not)

            // establish trump if necessary
        let trump =
            assert(
                playout.TrumpOpt.IsSome
                    || (playout.CurrentTrick.NumPlays = 0)
                        && playout.History = AbstractPlayoutHistory.empty)
            playout.TrumpOpt
                |> Option.defaultValue card.Suit

            // add card to trick
        let trick =
            playout.CurrentTrick
                |> AbstractTrick.addPlay trump card

            // complete trick if necessary
        let trickComplete = trick |> AbstractTrick.isComplete
        let history =
            if trickComplete then
                playout.History
                    |> AbstractPlayoutHistory.addTrick trick
            else playout.History

            // start a new trick if necessary
        let curTrick =
            if trickComplete
                && history |> AbstractPlayoutHistory.isComplete |> not then
                let lowTrumpRankOpt =
                    history.LowTakenOpt
                        |> Option.map fst
                trick
                    |> AbstractTrick.highPlayerIndex
                    |> AbstractTrick.create lowTrumpRankOpt
            else trick

            // assemble results
        {
            playout with
                TrumpOpt = Some trump
                History = history
                CurrentTrick = curTrick
        }

#if !FABLE_COMPILER
    /// String representation of a playout.
    let layout =
        [|
            AbstractPlayoutHistory.layout
            AbstractTrick.layout
        |] |> SpanLayout.combine

    /// String representation of a playout.
    let copyTo (span : Span<_>) handLowTrumpRankOpt playout =
        assert(playout |> isComplete |> not)
        assert(span.Length = layout.Length)

            // history
        let trick = playout.CurrentTrick
        let slice = layout.Slice(0, span)
        AbstractPlayoutHistory.copyTo
            slice
            handLowTrumpRankOpt
            trick
            playout.History

            // current trick
        let slice = layout.Slice(1, span)
        AbstractTrick.copyTo
            slice
            handLowTrumpRankOpt
            trick
#endif
