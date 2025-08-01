namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

open Trickster
open Trickster.Bots
open TestBots

module Trickster =

    let private toCloudHand hand =
        hand
            |> Seq.map (fun (card : Card) ->
                $"{card.Rank.Char}{card.Suit.Letter}")
            |> String.concat ""

    let private toCloudBid = function
        | Bid.Pass -> int cloud.BidBase.Pass
        | bid -> int PitchBid.Base + int bid

    let private ofCloudBid cloudBid =
        let bid = enum<Bid> (cloudBid - int PitchBid.Base)
        assert(Enum.getValues<Bid> |> Array.contains bid)
        bid

    /// Trickster Setback bot.
    let player =

        let bot =
            let options =
                cloud.PitchOptions(
                    dealerMaySteal = true,
                    isPartnership = true,
                    lowGoesToTaker = true,
                    minBid = 2,
                    players = 4,
                    setOpponentsAndWin = true,
                    stickTheDealer = false,
                    variation = cloud.PitchVariation.FourPoint)
            PitchBot(
                options,
                cloud.Suit.Unknown)

        let makeBid bidder hand auction =
            assert(Auction.isComplete auction |> not)
            let players =
                [|
                    let bidsMap =
                        auction
                            |> Auction.playerBids
                            |> Map
                    let indexedSeats =
                        Seq.indexed (Seat.cycle auction.Dealer.Next)
                    for iSeat, seat in indexedSeats do
                        let hand =
                            if seat = bidder then
                                toCloudHand hand
                            else ""
                        let bid =
                            bidsMap
                                |> Map.tryFind seat
                                |> Option.map toCloudBid
                                |> Option.defaultValue cloud.BidBase.NoBid
                        TestPlayer(
                            seat = iSeat,
                            bid = bid,
                            hand = hand)
                            :> cloud.PlayerBase
                |]
            let legalBids =
                Auction.legalBids auction
                    |> Seq.map (toCloudBid >> cloud.BidBase)
                    |> Seq.toArray
            let bidState =
                assert(players[auction.Bids.Length - 1].Hand <> "")
                cloud.SuggestBidState<cloud.PitchOptions>(
                    dealerSeat = players.Length - 1,
                    hand = cloud.Hand(toCloudHand hand),
                    legalBids = legalBids,
                    player = players[auction.Bids.Length - 1],
                    players = players)
            bot.SuggestBid(bidState).value |> ofCloudBid

        let makePlay player hand playout =
            assert(Playout.isComplete playout)
            let cardState =
                TestCardState(
                    bot,
                    players,
                    trick,
                    notLegal)
            bot.SuggestBid(cardState)

        let act infoSet =

            let legalActions = infoSet.LegalActions
            if legalActions.Length = 1 then
                Array.exactlyOne legalActions
            else
                match infoSet.Deal.PlayoutOpt with
                    | Some playout ->
                        makePlay
                            infoSet.Player infoSet.Hand playout
                            |> Play
                    | None ->
                        makeBid
                            infoSet.Player infoSet.Hand infoSet.Deal.Auction
                            |> Bid
(*
                let hand = infoSet.Hand
                let deal = infoSet.Deal
                let players =
                    [|
                        let bidsMap =
                            deal.Auction
                                |> Auction.playerBids
                                |> Map
                        let cardsTakenMap =
                            match deal.PlayoutOpt with
                                | Some playout ->
                                    playout
                                        |> Playout.tricks
                                        |> Seq.collect Trick.plays
                                        |> Seq.groupBy fst
                                        |> Seq.map (fun (seat, plays) ->
                                            seat, Seq.map snd plays)
                                        |> Map
                                | None -> Map.empty
                        for seat in Seat.cycle infoSet.Player do
                            let hand =
                                if seat = infoSet.Player then
                                    toString hand
                                else ""
                            let bids =
                                bidsMap
                                    |> Map.tryFind seat
                                    |> fun bid -> 
                                    |> Option.defaultValue cloud.BidBase.NoBid
                            let cardsTaken =
                                cardsTakenMap
                                    |> Map.tryFind seat
                                    |> Option.map toString
                                    |> Option.defaultValue ""
                            TestPlayer(
                                bid = moo,
                                hand = hand,
                                cardsTaken = cardsTaken)
                    |]

                let trick =
                    playout.CurrentTrickOpt
                        |> Option.map (fun trick ->
                            trick
                                |> Trick.plays
                                |> Seq.map snd
                                |> toString)
                        |> Option.defaultValue ""

                let notLegal =
                    (hand, legalActions)
                        ||> Seq.fold (fun hand card ->
                            assert(hand.Contains(card))
                            hand.Remove(card))
                        |> toString

                let card =
                    let cardState =
                        TestCardState(
                            bot,
                            players,
                            trick,
                            notLegal)
                    bot.SuggestNextCard(cardState)
                let rank = enum<Rank>(int card.rank)
                let suit =
                    match card.suit with
                        | cloud.Suit.Clubs -> Suit.Clubs
                        | cloud.Suit.Diamonds -> Suit.Diamonds
                        | cloud.Suit.Hearts -> Suit.Hearts
                        | cloud.Suit.Spades -> Suit.Spades
                        | _ -> failwith "Unexpected"
                Card(rank, suit)
*)

        { Act = act }
