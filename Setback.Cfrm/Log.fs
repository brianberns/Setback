namespace Setback.Cfrm

open PlayingCards
open Setback

open System
open System.IO

module Hand =

    /// Converts the given hand to a string.
    /// E.g. "K63♠ 2♥ 96♣"
    let toString (hand : Hand) =
        hand
            |> Seq.groupBy (fun card -> card.Suit)
            |> Seq.sortByDescending fst
            |> Seq.map (fun (suit, cards) ->
                let str =
                    cards
                        |> Seq.sortDescending
                        |> Seq.map (fun card -> card.Rank.Char)
                        |> Seq.toArray
                        |> String
                $"{str}{suit.Char}")
            |> String.concat " "

module Log =

    let create path (session : Session) =

        let wtr = new StreamWriter(path : string)

        let log = fprintf wtr

        let logn format =
            let finish () =
                wtr.WriteLine()
                wtr.Flush()
            Printf.kfprintf finish wtr format

        let onGameStart _ =
            logn "Game start"

        let onDealStart (dealer, deal) =
            logn ""
            for iSeat = 1 to Seat.numSeats do
                let seat = dealer |> Seat.incr iSeat
                let hand = deal.UnplayedCards.[iSeat % Seat.numSeats]
                logn $"{seat}: {Hand.toString hand}"

        let onAuctionStart _ =
            logn ""

        let onBid (seat : Seat, bid : Bid, deal : AbstractOpenDeal) =
            logn $"{seat}: {bid}"

        let onAuctionFinish _ =
            logn ""

        let onPlay (seat : Seat, card : Card, _) =
            log $"{seat.Char}:{card.Rank.Char}{card.Suit.Char} "

        let onTrickFinish _ =
            logn ""

        let onDealFinish (dealer : Seat, deal : AbstractOpenDeal, gameScore : AbstractScore) =

            let dealScore =
                deal
                    |> AbstractOpenDeal.dealScore
                    |> Game.shiftScore dealer
            logn ""
            logn "Deal points:"
            logn $"   E+W: {dealScore.[0]}"
            logn $"   N+S: {dealScore.[1]}"

            let gameScore =
                gameScore
                    |> Game.shiftScore dealer
            logn ""
            logn "Game score:"
            logn $"   E+W: {gameScore.[0]}"
            logn $"   N+S: {gameScore.[1]}"

        let onGameFinish (dealer : Seat, score) =
            logn ""
            logn "Game over"

        session.GameStartEvent.Add(onGameStart)
        session.DealStartEvent.Add(onDealStart)
        session.AuctionStartEvent.Add(onAuctionStart)
        session.BidEvent.Add(onBid)
        session.AuctionFinishEvent.Add(onAuctionFinish)
        session.PlayEvent.Add(onPlay)
        session.TrickFinishEvent.Add(onTrickFinish)
        session.DealFinishEvent.Add(onDealFinish)
        session.GameFinishEvent.Add(onGameFinish)
