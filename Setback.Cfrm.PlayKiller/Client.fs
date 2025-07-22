namespace Setback

open System
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
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
            assert(
                enum<Seat> message.Values[0]
                    = Seat.incr
                        (bidding.Deal.ClosedDeal.Auction.NumBids + 1)
                        bidding.Dealer)
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
                match message.Values[0] with
                    | -1 -> auction.HighBid.BidderIndex = -1
                    | idx ->
                        enum<Seat> idx =
                            Seat.incr
                                auction.HighBid.BidderIndex
                                playing.Dealer)
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
                let trick =
                    playing.Deal.ClosedDeal.Playout.CurrentTrick
                enum<Seat> message.Values[0]
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
 
    // Function to calculate the 95% confidence interval for a win rate
    let calculateConfidenceInterval (wins: int) (totalGames: int) =
        if totalGames = 0 then
            (0.0, 0.0)
        else
            let p = float wins / float totalGames
            let n = float totalGames
            let z = 1.96 // Z-score for 95% confidence
            let marginOfError = z * sqrt (p * (1.0 - p) / n)
            let lowerBound = p - marginOfError
            let upperBound = p + marginOfError
            (lowerBound, upperBound)

    // A reusable view for displaying a team's stats within a dynamic, zoomed-in range
    let teamStatsView (name: string) (wins: int) (totalGames: int) (viewMin: float) (viewMax: float) =
        let p = if totalGames = 0 then 0.5 else float wins / float totalGames
        let lower, upper = calculateConfidenceInterval wins totalGames
        let winRatePercent = p * 100.0

        let viewSpan = viewMax - viewMin
        // Avoid division by zero if the view has no span
        if viewSpan <= 0.0 then
            // If there's no range to display, show nothing
            DockPanel.create []
        else
            // Normalize the CI and win rate to the dynamic view range [viewMin, viewMax]
            let lower_norm = (max viewMin lower - viewMin) / viewSpan
            let upper_norm = (min viewMax upper - viewMin) / viewSpan
            let p_norm = (p - viewMin) / viewSpan

            let colDefs =
                let defs = ColumnDefinitions()
                // Column for the space before the CI bar
                defs.Add(ColumnDefinition(GridLength(lower_norm, GridUnitType.Star)))
                // Column for the CI bar itself
                defs.Add(ColumnDefinition(GridLength(upper_norm - lower_norm, GridUnitType.Star)))
                // Column for the space after the CI bar
                defs.Add(ColumnDefinition(GridLength(1.0 - upper_norm, GridUnitType.Star)))
                defs

            DockPanel.create [
                DockPanel.children [
                    TextBlock.create [
                        TextBlock.text (sprintf "%s: %d wins (%.2f%%)" name wins winRatePercent)
                        TextBlock.margin 5.0
                    ]

                    // Graphical representation using a Grid
                    Grid.create [
                        Grid.height 20.0
                        Grid.width 300.0
                        Grid.margin 5.0
                        Grid.background "lightgray"
                        Grid.columnDefinitions colDefs
                        Grid.children [
                            // The confidence interval bar
                            Border.create [
                                Border.background "cornflowerblue"
                                Grid.column 1
                            ]

                            // A marker for the actual win rate
                            Border.create [
                                Border.background "black"
                                Border.width 2.0
                                Border.horizontalAlignment HorizontalAlignment.Left
                                Grid.columnSpan 3
                                // Position the marker based on its normalized position
                                Border.margin (p_norm * 300.0 - 1.0, 0.0, 0.0, 0.0)
                            ]
                        ]
                    ]

                    TextBlock.create [
                        TextBlock.text (sprintf "95%% CI: [%.2f%%, %.2f%%]" (lower * 100.0) (upper * 100.0))
                        TextBlock.margin 5.0
                    ]
                ]
            ]


    // The main view of the application
    let view (model: Model) (dispatch: Message -> unit) =
        // Calculate the CIs for both teams to determine the zoom level
        let ewGamesWon, nsGamesWon = model.GamesWon
        let totalGames = ewGamesWon + nsGamesWon
        let ewLower, ewUpper = calculateConfidenceInterval ewGamesWon totalGames
        let nsLower, nsUpper = calculateConfidenceInterval nsGamesWon totalGames

        // Determine the total range covered by both CIs
        let minCI = min ewLower nsLower
        let maxCI = max ewUpper nsUpper
        
        // Add a 10% margin on each side for padding
        let margin = (maxCI - minCI) * 0.1
        
        // Calculate the final zoomed-in view range, clamped between 0 and 1
        let viewMin = max 0.0 (minCI - margin)
        let viewMax = min 1.0 (maxCI + margin)

        DockPanel.create [
            DockPanel.children [
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.text (sprintf "Total Games Played: %d" totalGames)
                    TextBlock.fontSize 24.0
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin 10.0
                ]
                
                // Display the current zoom range
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.text (sprintf "Viewing Range: [%.1f%%, %.1f%%]" (viewMin * 100.0) (viewMax * 100.0))
                    TextBlock.fontSize 14.0
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin (0.0, 0.0, 0.0, 10.0)
                ]

                StackPanel.create [
                    StackPanel.dock Dock.Top
                    StackPanel.horizontalAlignment HorizontalAlignment.Center
                    StackPanel.verticalAlignment VerticalAlignment.Center
                    StackPanel.spacing 20.0
                    StackPanel.children [
                        // Pass the calculated view range to each team's view
                        teamStatsView "E+W" ewGamesWon totalGames viewMin viewMax
                        teamStatsView "N+S" nsGamesWon totalGames viewMin viewMax
                    ]
                ]
            ]
        ]
