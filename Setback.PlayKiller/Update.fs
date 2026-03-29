namespace Setback.PlayKiller

open System
open System.IO

open Elmish

open PlayingCards
open Setback
open Setback.Model

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
            if message.Values[0] = 0                    // client plays E+W
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

    let private toScore ewScore nsScore =
        Score.create Team.EastWest ewScore
            + Score.create Team.NorthSouth nsScore

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
            GameScore = toScore ewScore nsScore
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
                            set cards)
                        |> OpenDeal.fromHands dealing.Dealer
                Playing {|
                    EwGamesWon = dealing.EwGamesWon
                    NsGamesWon = dealing.NsGamesWon
                    Dealer = dealing.Dealer
                    GameScore = dealing.GameScore
                    Deal = deal
                |}
        | model -> failwith $"Invalid state: {model}"

    let private deepCfrPlayer =
        let settings = Settings.create ()
        use model =
            new AdvantageModel(
                settings.HiddenSize,
                settings.NumHiddenLayers,
                0.0,
                TorchSharp.torch.CPU)   // always run on CPU
        model.load("AdvantageModel.pt") |> ignore
        model.eval()
        Strategy.createPlayer model

    let private onMakeBid message = function
        | Playing bidding ->
            assert(
                message.Values[0] =
                    int (
                        Auction.currentBidder
                            bidding.Deal.ClosedDeal.Auction))
            let bid =
                if message.Values[2] = -1 then
                    let bid =
                        let player =
                            OpenDeal.currentPlayer bidding.Deal
                        InformationSet.create
                            player
                            bidding.Deal.UnplayedCardMap[player]
                            bidding.Deal.ClosedDeal
                            bidding.GameScore
                            |> deepCfrPlayer.Act
                            |> Action.toBid
                    respond message.Key (int bid)
                    bid
                else
                    respond message.Key -1
                    enum<Bid> message.Values[1]
            let deal =
                bidding.Deal
                    |> OpenDeal.addBid bid
            Playing {| bidding with Deal = deal |}
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfBidding message = function
        | Playing playing as model ->
            let auction = playing.Deal.ClosedDeal.Auction
            assert(Auction.isComplete auction)
            assert(
                match message.Values[0], auction.HighBidderOpt with
                    | -1, None -> true
                    | idx, Some seat -> int seat = idx
                    | _ -> false)
            assert(
                message.Values[1] = int auction.HighBid)
            respond message.Key 0
            model
        | model -> failwith $"Invalid state: {model}"

    let private onStartOfTrick message = function
        | Playing playing as model ->
            match playing.Deal.ClosedDeal.PlayoutOpt with
                | Some playout ->
                    assert(
                        message.Values[0]
                            = int (
                                Playout.currentPlayer playout))
                    assert(
                        message.Values[1]
                            = playout.CompletedTricks.Length)
                    respond message.Key 0
                    model
                | None -> failwith "Unexpected"
        | model -> failwith $"Invalid state: {model}"

    let private onMakePlay message = function
        | Playing playing ->
            assert(
                match playing.Deal.ClosedDeal.PlayoutOpt with
                    | Some playout ->
                        match playout.CurrentTrickOpt with
                            | Some trick ->
                                message.Values[0] =
                                    int (Trick.currentPlayer trick)
                            | None -> false
                    | None -> false)
            let card =
                if message.Values[2] = -1 then
                    let card =
                        let player =
                            OpenDeal.currentPlayer playing.Deal
                        InformationSet.create
                            player
                            playing.Deal.UnplayedCardMap[player]
                            playing.Deal.ClosedDeal
                            playing.GameScore
                            |> deepCfrPlayer.Act
                            |> Action.toPlay
                    respond message.Key (Killer.toNum card)
                    card
                else
                    respond message.Key -1
                    Killer.toCard message.Values[1]
            let deal =
                playing.Deal
                    |> OpenDeal.addPlay card
            Playing {| playing with Deal = deal |}
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfTrick message = function
        | Playing playing as model ->
            assert(
                match playing.Deal.ClosedDeal.PlayoutOpt with
                    | Some playout ->
                        match playout.CurrentTrickOpt with
                            | Some trick ->
                                Playout.isComplete playout
                                    || trick.Cards.IsEmpty
                            | None -> false
                    | None -> false)
            respond message.Key 0
            model
        | model -> failwith $"Invalid state: {model}"

    let private onEndOfHand message = function
        | Playing playing ->
            assert(OpenDeal.isComplete playing.Deal)
            let ewScore = message.Values[0]
            let nsScore = message.Values[1]
            assert(
                let gameScore =
                    let dealScore =
                        ClosedDeal.getDealScore playing.Deal.ClosedDeal
                    playing.GameScore + dealScore
                toScore ewScore nsScore = gameScore)
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
                let gameScore =
                    let ewScore = message.Values[0]
                    let nsScore = message.Values[1]
                    toScore ewScore nsScore
                gameScore =
                    toScore complete.EwGameScore complete.NsGameScore
                    && Option.map int (Score.tryGetWinningTeam gameScore)
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
