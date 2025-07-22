namespace Setback

open System
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout

open PlayingCards
open Setback.Cfrm

type Model =

    | Start

    | Initialized

    | NewGameStarted of {|
        EwGamesWon : int
        NsGamesWon : int |}

    | Dealing of {|
        EwGamesWon : int
        NsGamesWon : int
        Dealer : Seat
        GameScore : AbstractScore
        NumCards : int
        CardMap : Map<Seat, Card[]> |}

    | Playing of {|
        EwGamesWon : int
        NsGamesWon : int
        Dealer : Seat
        GameScore : AbstractScore
        Deal : AbstractOpenDeal |}

    | HandComplete of {|
        EwGamesWon : int
        NsGamesWon : int
        EwGameScore : int
        NsGameScore : int |}

    | GameComplete of {|
        EwGamesWon : int
        NsGamesWon : int |}

    | Error of string

    member this.GamesWon =
        match this with
            | Start
            | Initialized -> 0, 0
            | NewGameStarted ngs -> ngs.EwGamesWon, ngs.NsGamesWon
            | Dealing dealing -> dealing.EwGamesWon, dealing.NsGamesWon
            | Playing playing -> playing.EwGamesWon, playing.NsGamesWon
            | HandComplete complete -> complete.EwGamesWon, complete.NsGamesWon
            | GameComplete complete -> complete.EwGamesWon, complete.NsGamesWon
            | Error _ -> failwith "Invalid state"

module Model =

    let init () = Start

type MessageKey =
    | Initialize = 1
    | StartNewGame = 2
    | DealNewHand = 3
    | First3Cards = 4
    | Second3Cards = 5
    | MakeBid = 6
    | EndOfBidding = 7
    | StartOfTrick = 8
    | MakePlay = 9
    | EndOfTrick = 10
    | EndOfHand = 11
    | EndOfGame = 12

type Message =
    {
        Key : MessageKey
        Values : int[]
    }

module Message =

    let private respond key value =
        let key = int key + 100
        Killer.writeMessage key value

    let private onInitialize message model =
        let response =
            if message.Values[0] = 0
                && message.Values[1] &&& 1 = 1          // play to 11
                && message.Values[1] &&& 2 = 0 then 0   // no smudge
            else -1
        respond message.Key response
        Initialized

    let private onStartNewGame message model =
        let ewGamesWon, nsGamesWon =
            match model with
                | Initialized -> 0, 0
                | GameComplete complete ->
                    complete.EwGamesWon, complete.NsGamesWon
                | _ -> failwith $"Invalid state: {model}"
        respond message.Key 0
        NewGameStarted {|
            EwGamesWon = ewGamesWon
            NsGamesWon = nsGamesWon |}

    let private toAbstractScore dealer ewScore nsScore =
        let points =
            match dealer with
                | Seat.West | Seat.East -> [| ewScore; nsScore |]
                | Seat.North | Seat.South -> [| nsScore; ewScore |]
                | _ -> failwith "Invalid dealer"
        AbstractScore points

    let private onDealNewHand message model =
        let ewGamesWon, nsGamesWon =
            match model with
                | NewGameStarted ngs ->
                    ngs.EwGamesWon, ngs.NsGamesWon
                | HandComplete complete ->
                    complete.EwGamesWon, complete.NsGamesWon
                | model -> failwith $"Invalid state: {model}"
        let dealer = enum<Seat> message.Values[0]
        let ewScore = message.Values[1]
        let nsScore = message.Values[2]
        respond message.Key 0
        Dealing {|
            EwGamesWon = ewGamesWon
            NsGamesWon = nsGamesWon
            Dealer = dealer
            GameScore =
                toAbstractScore
                    dealer ewScore nsScore
            NumCards = 0
            CardMap =
                Enum.getValues<Seat>
                    |> Seq.map (fun seat ->
                        seat, Array.empty)
                    |> Map
        |}

    let private updateCardMap (values : _[]) (cardMap : Map<_, _>) =
        let seat = enum<Seat> values[0]
        let cards =
            Array.append
                cardMap[seat]
                (Array.map Killer.toCard values[1..])
        Map.add seat cards cardMap

    let private onCardsReceived message = function
        | Dealing dealing ->
            respond message.Key 0
            let numCards = dealing.NumCards + 3
            let cardMap =
                updateCardMap
                    message.Values
                    dealing.CardMap
            if numCards < Setback.numCardsPerDeal then
                Dealing {|
                    dealing with
                        NumCards = numCards
                        CardMap = cardMap
                |}
            else
                let deal =
                    cardMap
                        |> Map.map (fun _ cards ->
                            Seq.ofArray cards)
                        |> AbstractOpenDeal.fromHands dealing.Dealer
                Playing {|
                    EwGamesWon = dealing.EwGamesWon
                    NsGamesWon = dealing.NsGamesWon
                    Dealer = dealing.Dealer
                    GameScore = dealing.GameScore
                    Deal = deal
                |}
        | model -> failwith $"Invalid state: {model}"

    let private dbPlayer =
        DatabasePlayer.player "Setback.db"

    let private onMakeBid message = function
        | Playing bidding ->
            let seat = enum<Seat> message.Values[0]
            assert(
                Seat.incr
                    (bidding.Deal.ClosedDeal.Auction.NumBids + 1)
                    bidding.Dealer = seat)
            let bid =
                if message.Values[2] = -1 then
                    let bid =
                        dbPlayer.MakeBid
                            bidding.GameScore bidding.Deal
                    respond message.Key (int bid)
                    bid
                else
                    respond message.Key -1
                    enum<Bid> message.Values[1]
            let deal =
                bidding.Deal
                    |> AbstractOpenDeal.addBid bid
            Playing {| bidding with Deal = deal |}
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfBidding message = function
        | Playing playing as model ->
            let auction = playing.Deal.ClosedDeal.Auction
            assert(AbstractAuction.isComplete auction)
            assert(
                message.Values[0]
                    = int (
                        Seat.incr
                            auction.HighBid.BidderIndex
                            playing.Dealer))
            assert(
                message.Values[1] = int auction.HighBid.Bid)
            respond message.Key 0
            model
        | model -> failwith $"Invalid state: {model}"

    let private onStartOfTrick message = function
        | Playing playing as model ->
            let playout = playing.Deal.ClosedDeal.Playout
            assert(
                message.Values[0]
                    = int (
                        Seat.incr
                            (AbstractPlayout.currentPlayerIndex
                                playout)
                            playing.Dealer))
            assert(
                message.Values[1]
                    = playout.History.NumTricksCompleted + 1)
            respond message.Key 0
            model
        | model -> failwith $"Invalid state: {model}"

    let private onMakePlay message = function
        | Playing playing ->
            assert(
                let seat = enum<Seat> message.Values[0]
                let trick =
                    playing.Deal.ClosedDeal.Playout.CurrentTrick
                seat
                    = Seat.incr
                        (trick.LeaderIndex + trick.NumPlays)
                        playing.Dealer)
            let card =
                if message.Values[2] = -1 then
                    let card =
                        dbPlayer.MakePlay
                            playing.GameScore playing.Deal
                    respond message.Key (Killer.toNum card)
                    card
                else
                    respond message.Key -1
                    Killer.toCard message.Values[1]
            let deal =
                playing.Deal
                    |> AbstractOpenDeal.addPlay card
            Playing {| playing with Deal = deal |}
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfTrick message = function
        | Playing playing as model ->
            assert(
                let playout = playing.Deal.ClosedDeal.Playout
                AbstractPlayout.isComplete playout
                    || playout.CurrentTrick.NumPlays = 0)
            respond message.Key 0
            model
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfHand message = function
        | Playing playing ->
            assert(
                AbstractOpenDeal.isExhausted playing.Deal)
            let ewScore = message.Values[0]
            let nsScore = message.Values[1]
            assert(
                let gameScore =
                    let dealScore =
                        AbstractOpenDeal.dealScore playing.Deal
                    playing.GameScore + dealScore
                toAbstractScore playing.Dealer ewScore nsScore
                    = gameScore)
            respond message.Key 0
            HandComplete {|
                EwGamesWon = playing.EwGamesWon
                NsGamesWon = playing.NsGamesWon
                EwGameScore = ewScore
                NsGameScore = nsScore
            |}
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfGame message = function
        | HandComplete complete ->
            let winningTeamIdx = message.Values[2]
            assert(
                let dummyDealer = Seat.West
                let gameScore =
                    let ewScore = message.Values[0]
                    let nsScore = message.Values[1]
                    toAbstractScore dummyDealer ewScore nsScore
                gameScore =
                    toAbstractScore
                        dummyDealer complete.EwGameScore complete.NsGameScore
                    && BootstrapGameState.winningTeamOpt gameScore
                        = Some winningTeamIdx)
            let ewGamesWon, nsGamesWon =
                match winningTeamIdx with
                    | 0 -> complete.EwGamesWon + 1, complete.NsGamesWon
                    | 1 -> complete.EwGamesWon, complete.NsGamesWon + 1
                    | _ -> failwith $"Invalid team index: {winningTeamIdx}"
            respond message.Key 0
            GameComplete {|
                EwGamesWon = ewGamesWon
                NsGamesWon = nsGamesWon
            |}
        | model -> failwith $"Invalid state: {model}"

    let update message model =
        try
            match message.Key with
                | MessageKey.Initialize -> onInitialize message model
                | MessageKey.StartNewGame -> onStartNewGame message model
                | MessageKey.DealNewHand -> onDealNewHand message model
                | MessageKey.First3Cards
                | MessageKey.Second3Cards -> onCardsReceived message model
                | MessageKey.MakeBid -> onMakeBid message model
                | MessageKey.EndOfBidding -> onEndOfBidding message model
                | MessageKey.StartOfTrick -> onStartOfTrick message model
                | MessageKey.MakePlay -> onMakePlay message model
                | MessageKey.EndOfTrick -> onEndOfTrick message model
                | MessageKey.EndOfHand -> onEndOfHand message model
                | MessageKey.EndOfGame -> onEndOfGame message model
                | _ -> failwith $"Unexpected message key: {message.Key}"
        with exn -> Error exn.Message

    let private onMasterFileChanged dispatch _args =
        let tokens = Killer.readMessage ()
        dispatch {
            Key = enum<MessageKey> tokens[0]
            Values = tokens[1..]
        }

    let private watch : Subscribe<_> =
        fun dispatch ->

            let watcher =
                new FileSystemWatcher(
                    @"C:\Program Files\KSetback",
                    "KSetback.msg.master",
                    EnableRaisingEvents = true)
            watcher.Changed.Add(
                onMasterFileChanged dispatch)

            {
                new IDisposable with
                    member _.Dispose() =
                        watcher.Dispose()
            }

    let subscribe model : Sub<_> =
        [
            [ "watch" ], watch
        ]

 module View =

    let createTextBlock column text =
        TextBlock.create [
            TextBlock.text text
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.verticalAlignment VerticalAlignment.Center
            TextBlock.margin 10.0
            Grid.column column
        ]

    let view (model : Model) dispatch =
        Grid.create [
            match model with
                | Error msg ->
                    Grid.children [
                        createTextBlock 0 msg
                    ]
                | _ ->
                    Grid.columnDefinitions "*, *"
                    Grid.children [
                        let ewGamesWon, nsGamesWon = model.GamesWon
                        createTextBlock 1 $"E+W games won: {ewGamesWon}"
                        createTextBlock 0 $"N+S games won: {nsGamesWon}"
                    ]
        ]
