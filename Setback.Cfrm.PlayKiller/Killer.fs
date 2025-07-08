namespace Setback

open System
open System.IO

open PlayingCards
open Setback
open Setback.Cfrm

/// Logs messages to both console and file.
type Log() =

    static member WriteLine format =
        let writeline (message : string) =
            Console.WriteLine message
            use wtr = new IO.StreamWriter("MSetback.log", true)
            wtr.WriteLine message
        Printf.ksprintf writeline format

/// Killer Setback integration
module Killer =

    let folder = "C:\Program Files\KSetback"

    /// Tokenizes the contents of the given file.
    let tokenize (fileInfo : FileInfo) =
        
        let mutable rdr : StreamReader = null
        while rdr = null do
            try
                rdr <- fileInfo.OpenText()
            with
                ex -> Threading.Thread.Sleep 0
        let tokens =
            rdr.ReadToEnd().Trim().Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                |> Seq.map (fun token -> Int32.Parse(token))
                |> Seq.toArray
        rdr.Dispose()
        tokens

    /// Receives a message from KS.
    let readMessage key =
        // Log.WriteLine "Read message %A" key
        let fileInfo = new FileInfo(System.IO.Path.Combine(folder, "KSetback.msg.master"))
        while not fileInfo.Exists do
            fileInfo.Refresh()
        let tokens = tokenize fileInfo
        if tokens.[0] <> key then failwith (sprintf "Expected key: %d, received key: %d" key tokens.[0])
        fileInfo.Delete()
        tokens.[1], tokens.[2], tokens.[3], tokens.[4]

    /// Sends a message to KS.
    let writeMessage key value =
        // Log.WriteLine "Write message %A" key
        let mutable wtr : StreamWriter = null
        while wtr = null do
            try
                wtr <- new IO.StreamWriter(System.IO.Path.Combine(folder, "KSetback.msg.slave"))
            with
                ex -> Threading.Thread.Sleep 0
        let message = sprintf "%d %d" key value
        wtr.Write message
        wtr.Dispose()

    /// Establishes initial communication with KS.
    let handshake () =
        Log.WriteLine "Killer Setback handshake"
        let fileInfo = new FileInfo(System.IO.Path.Combine(folder, "KSetback.msg.master"))
        if fileInfo.Exists then
            let tokens = tokenize fileInfo
            if tokens.[0] <> 1 then
                fileInfo.Delete()
        readMessage 1 |> ignore
        writeMessage 101 0

    /// Converts the given KS card into a native card.
    let asCard cardNum =
        let suit =
            match cardNum % 4 with
                | 0 -> Suit.Spades
                | 1 -> Suit.Hearts
                | 2 -> Suit.Clubs
                | 3 -> Suit.Diamonds
                | _ -> failwith "Unexpected suit"
        let rank = enum<Rank> ((cardNum / 4) + 2)
        Card(rank, suit)

    /// Converts the given native card into a KS card.
    let asNum (card : Card) =
        let suitNum =
            match card.Suit with
                | Suit.Spades -> 0
                | Suit.Hearts -> 1
                | Suit.Clubs -> 2
                | Suit.Diamonds -> 3
                | _ -> failwith "Unexpected suit"
        let rankNum = (int card.Rank) - 2
        (rankNum * 4) + suitNum

    type AbstractOpenDeal with
        member this.Auction = this.ClosedDeal.Auction

    type AbstractAuction with
        member this.Length = this.NumBids

    /// Receives bid from KS.
    let receiveBid (deal : AbstractOpenDeal) =
        let bidderNum, masterBidNum, slaveBidNum, _ = readMessage 6
        if slaveBidNum <> 0 then failwith "Unexpected slave bid"
        let bidderSeat = enum<Seat> bidderNum
        if bidderSeat <> deal.NextBidder then failwith "Unexpected bidder"
        let bid = enum<Bid> masterBidNum
        writeMessage 106 -1
        bid

    /// Hack to keep in sync with KS.
    let sync (deal : AbstractOpenDeal) =
        if deal.Auction.Length = Seat.numSeats then
            if deal.Tricks.IsEmpty then
                let killerBidderNum, killerBidNum, _, _ = readMessage 7
                if killerBidderNum = -1 then
                    if deal.HighBidderOpt <> None then failwith "Unexpected auction"
                else
                    let killerBidder = enum<Seat> killerBidderNum
                    let killerBid = enum<Bid> killerBidNum
                    let localBidder = deal.HighBidderOpt.Value
                    let localBid = deal.HighBid
                    if killerBidder <> localBidder then failwith "Unexpected auction winner"
                    if killerBid <> localBid then failwith "Unexpected bid"
                writeMessage 107 0
            let localTrickOpt, localPlayer = deal.NextPlayer
            if localTrickOpt.IsNone then
                if not deal.Tricks.IsEmpty then
                    readMessage 10 |> ignore
                    writeMessage 110 0
                let killerLeaderNum, killerTrickNum, _, _ = readMessage 8
                let killerLeader = enum<Seat> killerLeaderNum
                if killerLeader <> localPlayer then failwith "Unexpected leader"
                if killerTrickNum <> deal.Tricks.Length + 1 then failwith "Unexpected trick number"
                writeMessage 108 0

    /// Receives a play from KS.
    let receivePlay deal =
        sync deal
        let playerNum, masterCardNum, slaveCardNum, _ = readMessage 9
        if slaveCardNum <> 0 then failwith "Unexpected slave play"
        let playerSeat = enum<Seat> playerNum
        if playerSeat <> snd deal.NextPlayer then failwith "Unexpected player"
        let card = asCard masterCardNum
        writeMessage 109 -1
        card

    /// Sends a bid to KS.
    let sendBid deal (player : IPlayer) =
        let bid = player.Bid deal
        let bidderNum, masterBidNum, slaveBidNum, _ = readMessage 6
        if masterBidNum <> -1 then failwith "Unexpected master bid"
        let bidderSeat = enum<Seat> bidderNum
        if bidderSeat <> deal.NextBidder then failwith "Unexpected bidder"
        writeMessage 106 (int bid)
        bid

    /// Sends a play to KS.
    let sendPlay deal (player : IPlayer) =
        sync deal
        let card = player.Play deal
        let playerNum, masterCardNum, slaveCardNum, _ = readMessage 9
        if masterCardNum <> -1 then failwith "Unexpected master play"
        let playerSeat = enum<Seat> playerNum
        if playerSeat <> snd deal.NextPlayer then failwith "Unexpected player"
        writeMessage 109 (asNum card)
        card

    /// KS "master" player.
    type MasterPlayer() =
        interface IPlayer with
            member this.Bid deal =
                let bid = receiveBid deal
                // Log.WriteLine "Master bid: %A" bid
                bid
            member this.Play deal =
                let play = receivePlay deal
                // Log.WriteLine "Master play: %A" play
                play

    /// KS "slave" player.
    type SlavePlayer(player : IPlayer) =
        member this.InnerPlayer = player
        interface IPlayer with
            member this.Bid deal =
                let bid = sendBid deal player
                // Log.WriteLine "Slave bid: %A" bid
                bid
            member this.Play deal =
                let play = sendPlay deal player
                // Log.WriteLine "Slave play: %A" play
                play

    /// Wraps a game for use with KS.  
    type GameWrapper = { Game : Game }

    let teamNS = { Seats = [ Seat.North; Seat.South ]; Number = 0}
    let teamEW = { Seats = [ Seat.East ; Seat.West  ]; Number = 1 }

    let createWrapper players =
        readMessage 2 |> ignore
        writeMessage 102 0
        { Game = Game.create players [|teamNS; teamEW|] }

    let syncScore ewScore nsScore wrapper =
        let score = wrapper.Game.Score
        if score.[teamEW] <> ewScore then failwith "Invalid EW score"
        if score.[teamNS] <> nsScore then failwith "Invalid NS score"

    let playHand wrapper =

        let dealerNum, ewScore, nsScore, _ = readMessage 3
        let dealerSeat = enum<Seat> dealerNum
        syncScore ewScore nsScore wrapper
        writeMessage 103 0

        let hands =
            [0..7]
                |> Seq.collect (fun i ->
                    let msgNum = 4 + (i / 4)
                    let seatNum, cardNum1, cardNum2, cardNum3 = readMessage msgNum
                    let seat = enum<Seat> seatNum
                    let cards = 
                        [
                            (seat, asCard cardNum1)
                            (seat, asCard cardNum2)
                            (seat, asCard cardNum3)
                        ]
                    writeMessage (msgNum + 100) 0
                    cards)
                |> Seq.groupBy (fun (seat, _) -> seat)
                |> Seq.sortBy (fun (seat, _) -> seat)
                |> Seq.map (fun (_, pairs) -> pairs |> Seq.map snd |> Seq.toArray)
                |> Seq.toArray

        let teams = [| teamEW; teamNS |]
        let deal = OpenDeal.fromHands teams dealerSeat hands
        wrapper.Game.PlayDeal deal

    let play i wrapper =

        let rec loop wrapper =

            let innerGame, matchPoints = wrapper |> playHand

            if matchPoints.Points |> Seq.forall (fun points -> points = 0) then
                let bidderNum, bidNum, _, _ = readMessage 7
                if bidderNum <> -1 then failwith "Unexpected bidder"
                if bidNum <> 0 then failwith "Unexpected bid"
                writeMessage 107 0
            else
                readMessage 10 |> ignore
                writeMessage 110 0

            let ewScore, nsScore, _, _ = readMessage 11
            let wrapper = { Game = innerGame }
            wrapper |> syncScore ewScore nsScore
            writeMessage 111 0

            match innerGame.WinningTeam with
                | None -> loop wrapper
                | Some localTeam ->
                    let ewScore, nsScore, killerTeamNum, _ = readMessage 12
                    wrapper |> syncScore ewScore nsScore
                    Log.WriteLine ""
                    match killerTeamNum with
                        | 0 when localTeam = teamEW -> Log.WriteLine "E+W wins"
                        | 1 when localTeam = teamNS -> Log.WriteLine "N+S wins"
                        | _ -> failwith "Unexpected winning team"
                    Log.WriteLine ""
                    Log.WriteLine "──────────────────────────────────────────────────────────────────"
                    writeMessage 112 0
                    wrapper
                    
        loop wrapper

    let parseHand (rdr : TextReader) =

        let line = rdr.ReadLine()
        let tokens = line.Split([| ':'; ' ' |], StringSplitOptions.RemoveEmptyEntries)
        let seat = Seat.fromChar tokens.[0].[0]
        let cards =
            [Suit.Spades; Suit.Hearts; Suit.Clubs; Suit.Diamonds]
                |> Seq.zip (tokens |> Seq.skip 1)
                |> Seq.collect (fun (token, suit) ->
                    if token = "-" then
                        Seq.empty
                    else
                        token |> Seq.map (fun c -> { Rank = Rank.fromChar c; Suit = suit }))
                |> Seq.toArray
        (seat, cards)

    let parseAuction (rdr : TextReader) =

        let parseSlaveBid () =
            let line = rdr.ReadLine()
            let tokens = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
            let seat = Enum.Parse(typeof<Seat>, tokens.[1]) :?> Seat
            let bid = tokens.[3] |> Int32.Parse |> enum<Bid>
            (seat, bid)
        let slaveBids =
            [0..1]
                |> Seq.map (fun _ -> parseSlaveBid ())
                |> Map.ofSeq

        let line = rdr.ReadLine()
        let tokens = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
        let seatBids =
            tokens
                |> Seq.skip 1
                |> Seq.map (fun token ->
                    let subtokens = token.Split([| ':' |])
                    let seat = Seat.fromChar subtokens.[0].[0]
                    let bid =
                        match subtokens.[1] with
                            | "P" -> Bid.Pass
                            | str -> str.[0].ToString() |> Int32.Parse |> enum<Bid>
                    (seat, bid))
        seatBids
            |> Seq.map (fun (seat, bid) ->
                if slaveBids.ContainsKey seat then
                    (seat, slaveBids.[seat])
                else
                    (seat, bid))
            |> Seq.toArray

    let parseTrick (deal : OpenDeal) (rdr : TextReader) =

        let line = rdr.ReadLine()
        let tokens = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
        let plays =
            tokens
                |> Seq.skip 2
                |> Seq.map (fun token ->
                    let subtokens = token.Split([| ':' |])
                    let seat = Seat.fromChar subtokens.[0].[0]
                    let card = Card.fromString subtokens.[1]
                    (seat, card))
        (deal, plays)
            ||> Seq.fold (fun deal (seat, card) ->
                if (deal.NextPlayer |> snd) <> seat then
                    failwith "Unexpected player"
                deal.AddPlay card)

    let parseDealPartial (rdr : TextReader) =

        let mutable matched = false
        while not matched do
            let line = rdr.ReadLine()
            let tokens = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
            if tokens.Length = 4 then
                let pairs = tokens |> Seq.zip([ "spades"; "hearts"; "clubs"; "diamonds" ])
                matched <- pairs |> Seq.forall (fun (a, b) -> a = b)

        let hands =
            [1..4]
                |> Seq.map (fun _ -> parseHand rdr)
                |> Seq.sortBy fst
                |> Seq.map snd
                |> Seq.toArray

        let seatBids = parseAuction rdr
        let dealer = seatBids |> Seq.last |> fst
        let teams = [| teamEW; teamNS |]
        let deal = OpenDeal.fromHands teams dealer hands
        let deal =
            (deal, seatBids)
                ||> Seq.fold (fun deal (_, bid) -> deal.AddBid bid)

        (deal, [1..OpenDeal.numCardsPerHand])
            ||> Seq.fold (fun deal _ ->
                if rdr.Peek() = -1 then
                    deal
                else
                    parseTrick deal rdr)

    let parseDeal (rdr : TextReader) =

        let deal = parseDealPartial rdr

        let line = rdr.ReadLine()
        let tokens = line.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
        let id = tokens |> Seq.last |> Int32.Parse

        (id, deal)

    type GameWrapper with
        member this.PlayHand = this |> playHand
        member this.Play i = this |> play i
