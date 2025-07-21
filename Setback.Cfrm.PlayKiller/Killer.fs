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
            use wtr = new IO.StreamWriter("Setback.log", true)
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
        if tokens[0] <> key then failwith (sprintf "Expected key: %d, received key: %d" key tokens[0])
        fileInfo.Delete()
        tokens[1], tokens[2], tokens[3], tokens[4]

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
            if tokens[0] <> 1 then
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
        member this.NextBidder = failwith "Not implemented"
        member this.Tricks : List<_> = failwith "Not implemented"
        member this.HighBidderOpt = failwith "Not implemented"
        member this.HighBid = failwith "Not implemented"
        member this.NextPlayer : Option<_> * _ = failwith "Not implemented"

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
    let sendBid deal (score : AbstractScore) (player : Player) =
        let bid = player.MakeBid score deal
        let bidderNum, masterBidNum, slaveBidNum, _ = readMessage 6
        if masterBidNum <> -1 then failwith "Unexpected master bid"
        let bidderSeat = enum<Seat> bidderNum
        if bidderSeat <> deal.NextBidder then failwith "Unexpected bidder"
        writeMessage 106 (int bid)
        bid

    /// Sends a play to KS.
    let sendPlay deal (score : AbstractScore) (player : Player) =
        sync deal
        let card = player.MakePlay score deal
        let playerNum, masterCardNum, slaveCardNum, _ = readMessage 9
        if masterCardNum <> -1 then failwith "Unexpected master play"
        let playerSeat = enum<Seat> playerNum
        if playerSeat <> snd deal.NextPlayer then failwith "Unexpected player"
        writeMessage 109 (asNum card)
        card

    /// KS "master" player.
    let masterPlayer =
        {
            MakeBid =
                fun _score deal ->
                    let bid = receiveBid deal
                    // Log.WriteLine "Master bid: %A" bid
                    bid
            MakePlay =
                fun _score deal ->
                    let play = receivePlay deal
                    // Log.WriteLine "Master play: %A" play
                    play
        }

    /// KS "slave" player.
    let slavePlayer =
        let dbPlayer = DatabasePlayer.player "Setback.db"
        {
            MakeBid =
                fun score deal ->
                    let bid = sendBid deal score dbPlayer
                    // Log.WriteLine "Slave bid: %A" bid
                    bid
            MakePlay =
                fun score deal ->
                    let play = sendPlay deal score dbPlayer
                    // Log.WriteLine "Slave play: %A" play
                    play
        }

    let playerMap =
        Map [
            Seat.West, slavePlayer
            Seat.North, masterPlayer
            Seat.East, slavePlayer
            Seat.South, masterPlayer
        ]

    let session =
        let dummyRng : Random = null
        Session(playerMap, dummyRng)

    /// Wraps a game for use with KS.  
    type GameWrapper = { Game : Game }

    let createWrapper () =
        readMessage 2 |> ignore
        writeMessage 102 0
        { Game = Game.zero }

    let syncScore ewScore nsScore wrapper =
        let score = wrapper.Game.Score
        if score[0] <> ewScore then failwith "Invalid EW score"
        if score[1] <> nsScore then failwith "Invalid NS score"

    let playDeal wrapper =

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
                    let seatCards = 
                        [
                            seat, asCard cardNum1
                            seat, asCard cardNum2
                            seat, asCard cardNum3
                        ]
                    writeMessage (msgNum + 100) 0
                    seatCards)
                |> Seq.groupBy fst
                |> Seq.map (fun (seat, pairs) ->
                    seat,
                    pairs |> Seq.map snd)
                |> Map

        let deal = AbstractOpenDeal.fromHands dealerSeat hands
        let game = session.PlayDeal(dealerSeat, deal, wrapper.Game)
        { wrapper with Game = game }

    let play wrapper =

        let rec loop wrapper =

            let wrapper = wrapper |> playDeal

            let (AbstractScore points) = wrapper.Game.Score
            if points |> Seq.forall (fun points -> points = 0) then
                let bidderNum, bidNum, _, _ = readMessage 7
                if bidderNum <> -1 then failwith "Unexpected bidder"
                if bidNum <> 0 then failwith "Unexpected bid"
                writeMessage 107 0
            else
                readMessage 10 |> ignore
                writeMessage 110 0

            let ewScore, nsScore, _, _ = readMessage 11
            let wrapper =
                { wrapper with
                    Game = {
                        Score =
                            AbstractScore [| ewScore; nsScore |]} }
            wrapper |> syncScore ewScore nsScore
            writeMessage 111 0

            match BootstrapGameState.winningTeamOpt wrapper.Game.Score with
                | None -> loop wrapper
                | Some localTeam ->
                    let ewScore, nsScore, killerTeamNum, _ = readMessage 12
                    wrapper |> syncScore ewScore nsScore
                    Log.WriteLine ""
                    match killerTeamNum with
                        | 0 when localTeam = 0 -> Log.WriteLine "E+W wins"
                        | 1 when localTeam = 1 -> Log.WriteLine "N+S wins"
                        | _ -> failwith "Unexpected winning team"
                    Log.WriteLine ""
                    Log.WriteLine "──────────────────────────────────────────────────────────────────"
                    writeMessage 112 0
                    wrapper
                    
        loop wrapper
