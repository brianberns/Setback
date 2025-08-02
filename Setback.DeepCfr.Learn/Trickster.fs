namespace Setback.DeepCfr.Learn

open PlayingCards
open Setback

open Trickster
open Trickster.Bots
open TestBots

module Trickster =

    let private ofCloudRank (rank : cloud.Rank) =
        enum<Rank>(int rank)

    let private toCloudSuit = function
        | Suit.Clubs -> cloud.Suit.Clubs
        | Suit.Diamonds -> cloud.Suit.Diamonds
        | Suit.Hearts -> cloud.Suit.Hearts
        | Suit.Spades -> cloud.Suit.Spades
        | _ -> failwith "Unexpected"

    let private ofCloudSuit = function
        | cloud.Suit.Clubs -> Suit.Clubs
        | cloud.Suit.Diamonds -> Suit.Diamonds
        | cloud.Suit.Hearts -> Suit.Hearts
        | cloud.Suit.Spades -> Suit.Spades
        | _ -> failwith "Unexpected"

    let private toCloudCards cards =
        cards
            |> Seq.map (fun (card : Card) ->
                $"{card.Rank.Char}{card.Suit.Letter}")
            |> String.concat ""

    let private ofCloudCard (card : cloud.Card) =
        let rank = ofCloudRank card.rank
        let suit = ofCloudSuit card.suit
        Card(rank, suit)

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

        let chooseTrump hand =
            let cloudHand =
                cloud.Hand(toCloudCards hand)
            Enum.getValues<Suit>
                |> Seq.map toCloudSuit
                |> Seq.maxBy (fun cloudSuit ->
                    bot.EstimatedPoints(cloudHand, cloudSuit))
                |> ofCloudSuit

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
                                toCloudCards hand
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
                    hand = cloud.Hand(toCloudCards hand),
                    legalBids = legalBids,
                    player = players[auction.Bids.Length - 1],
                    players = players)
            bot.SuggestBid(bidState).value |> ofCloudBid

        let makePlay player hand auction playout =
            assert(Playout.isComplete playout |> not)
            let cloudTrump =
                playout.TrumpOpt
                    |> Option.defaultWith (fun () ->
                        chooseTrump hand)
                    |> toCloudSuit
            let players =
                [|
                    let cardsTakenMap =
                        playout
                            |> Playout.tricks
                            |> Seq.collect Trick.plays
                            |> Seq.groupBy fst
                            |> Seq.map (fun (seat, plays) ->
                                seat, Seq.map snd plays)
                            |> Map
                    for seat in Seat.cycle player do
                        let hand =
                            if seat = player then
                                toCloudCards hand
                            else ""
                        let bid =
                            if Some player = auction.HighBidderOpt then
                                assert(auction.HighBid <> Bid.Pass)
                                int PitchBid.Pitching
                                    + (10 * int (toCloudBid auction.HighBid))
                                    + (int cloudTrump)
                            else
                                int PitchBid.NotPitching
                        let cardsTaken =
                            cardsTakenMap
                                |> Map.tryFind seat
                                |> Option.map toCloudCards
                                |> Option.defaultValue ""
                        TestPlayer(
                            bid = bid,
                            hand = hand,
                            cardsTaken = cardsTaken)
                |]

            let trick =
                playout.CurrentTrickOpt
                    |> Option.map (fun trick ->
                        trick
                            |> Trick.plays
                            |> Seq.map snd
                            |> toCloudCards)
                    |> Option.defaultValue ""

            let notLegal =
                (set hand, Playout.legalPlays hand playout)
                    ||> Seq.fold (fun hand card ->
                        assert(hand.Contains(card))
                        hand.Remove(card))
                    |> toCloudCards

            let cardState =
                TestCardState(
                    bot,
                    players,
                    trick,
                    notLegal,
                    trumpSuit = cloudTrump,
                    trumpAnytime = true)
            bot.SuggestNextCard(cardState)
                |> ofCloudCard

        let act infoSet =

            let legalActions = infoSet.LegalActions
            if legalActions.Length = 1 then
                Array.exactlyOne legalActions
            else
                let player = infoSet.Player
                let hand = infoSet.Hand
                let auction = infoSet.Deal.Auction
                match infoSet.Deal.PlayoutOpt with
                    | Some playout ->
                        makePlay player hand auction playout
                            |> MakePlay
                    | None ->
                        makeBid player hand auction
                            |> MakeBid

        { Act = act }
