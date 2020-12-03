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

        let onGameStart () =
            logn "Game start"

        let onDealStart (dealer, deal) =
            logn ""
            for iSeat = 1 to Seat.numSeats do
                let seat = dealer |> Seat.incr iSeat
                let hand = deal.UnplayedCards.[iSeat % Seat.numSeats]
                logn $"{seat}: {Hand.toString hand}"

        let onAuctionStart (leader : Seat) =
            logn ""

        let onBid (seat : Seat, bid : Bid, deal : AbstractOpenDeal) =
            logn $"{seat}: {bid}"

        let onAuctionFinish () =
            logn ""

        let onPlay (seat : Seat, card : Card, deal : AbstractOpenDeal) =
            log $"{seat.Char}:{card.Rank.Char}{card.Suit.Char} "

        let onTrickFinish () =
            logn ""

        let onGameFinish (dealer, score) =
            logn "over"

        session.GameStartEvent.Add(onGameStart)
        session.DealStartEvent.Add(onDealStart)
        session.AuctionStartEvent.Add(onAuctionStart)
        session.BidEvent.Add(onBid)
        session.AuctionFinishEvent.Add(onAuctionFinish)
        session.PlayEvent.Add(onPlay)
        session.TrickFinishEvent.Add(onTrickFinish)
        session.GameFinishEvent.Add(onGameFinish)
