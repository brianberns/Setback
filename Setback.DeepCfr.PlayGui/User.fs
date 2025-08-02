namespace Setback.DeepCfr.PlayGui

open System.Threading

open PlayingCards
open Setback

module User =

    /// Creates user player.
    let player
        (bidControl : BidControl)
        (handControl : HandControl)
        (actionQueue : ActionQueue) =

        /// User's selected bid.
        let mutable selectedBidOpt = Option<Bid>.None

        /// User's selected card.
        let mutable selectedCardOpt = Option<Card>.None

        /// Thread synchronization.
        let waitEvent = new AutoResetEvent(false)

        /// Allows the user to bid on the given deal. This executes
        /// on the main thread.
        let allowBid deal =
            let legalBids =
                deal.Auction
                    |> Auction.legalBids
                    |> set
            for bid in Enum.getValues<Bid> do
                let rb = bidControl.GetBidButton(bid)
                rb.Checked <- false
                rb.Enabled <- legalBids.Contains(bid)
            bidControl.Visible <- true

        /// Obtains user's bid. This executes on the worker thread.
        let makeBid (_ : Score) deal =
            selectedBidOpt <- None
            actionQueue.Enqueue (fun () -> allowBid deal)
            waitEvent.WaitOne() |> ignore   // wait for user selection
            selectedBidOpt.Value

        /// User has selected a bid.
        let onBidSelected bid =
            bidControl.Visible <- false
            selectedBidOpt <- Some bid
            waitEvent.Set() |> ignore   // allow worker thread to continue

        /// Allows the user to play a card. This executes on the main
        /// thread.
        let allowPlay hand deal =
            match deal.PlayoutOpt with
                | Some playout ->
                    let legalPlays =
                        playout
                            |> Playout.legalPlays hand
                            |> set
                    for cardCtrl in handControl.CardControls do
                        cardCtrl.IsClickable <-
                            cardCtrl.CardOpt
                                |> Option.map (fun card ->
                                    legalPlays.Contains(card))
                                |> Option.defaultValue false
                | None -> failwith "Unexpected"

        /// Obtains user's play. This executes on the worker thread.
        let makePlay (_ : Score) hand deal =
            selectedCardOpt <- None
            actionQueue.Enqueue (fun () -> allowPlay hand deal)
            waitEvent.WaitOne() |> ignore   // wait for user selection
            selectedCardOpt.Value

        /// User has selected a card.
        let onCardSelected card =
            for cardCtrl in handControl.CardControls do
                cardCtrl.IsClickable <- false
            selectedCardOpt <- Some card
            waitEvent.Set() |> ignore   // allow worker thread to continue

        bidControl.BidSelectedEvent.Add(onBidSelected)
        handControl.CardSelectedEvent.Add(onCardSelected)

        let act infoSet =
            if infoSet.Deal.PlayoutOpt.IsSome then
                makePlay Score.zero infoSet.Hand infoSet.Deal
                    |> MakePlay
            else
                makeBid Score.zero infoSet.Deal
                    |> MakeBid

        { Act = act }
