namespace Setback.DeepCfr.PlayGui

open System
open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Main form for playing Setback:
///    * One hand control per seat, arranged roughly in a circle
///    * A bid control next to the users's hand control
///    * The user's "Go" button for advancing the game
type MainForm() as this =
    inherit Form(
        Text = "Bernsrite Setback",
        Size = Size(1465, 937),
        StartPosition = FormStartPosition.CenterScreen,
        BackColor = Color.DarkGreen)

    /// Unplayed cards for each seat.
    let handControlMap =
        Seat.allSeats
            |> Seq.map (fun seat ->
                let ctrl =
                    new HandControl(
                        seat,
                        ShowFront = (seat = Seat.South))
                        |> Control.addTo this
                seat, ctrl)
            |> Map

    /// User's bid control.
    let bidControl =
        new BidControl(
            Visible = false)
            |> Control.addTo this

    /// Current trick.
    let trickControl =
        new TrickControl()
            |> Control.addTo this

    /// User must click to continue.
    let goButton =
        new Button(
            Size = Size(100, 30),
            Text = "Go",
            Font = new Font("Calibri", 12f),
            UseVisualStyleBackColor = true,
            Visible = false)
            |> Control.addTo this

    /// Team scoreboard.
    let scoreControl =
        new ScoreControl()
            |> Control.addTo this

    /// Action queue for delaying event handlers.
    let actionQueue = ActionQueue(250, this)

    /// Lays out controls.
    let onResize _ =

        let padding = 50

            // hand controls
        let vertLeft =
            (this.ClientSize.Width - HandControl.Width) / 2
        let horizTop =
            (this.ClientSize.Height - HandControl.Height - goButton.Height) / 2
        handControlMap[Seat.North].Location <-
            Point(vertLeft, padding)
        handControlMap[Seat.South].Location <-
            let yCoord =
                this.ClientSize.Height - HandControl.Height - goButton.Height - padding
            Point(vertLeft, yCoord)
        handControlMap[Seat.East].Location <-
            let xCoord =
                this.ClientSize.Width - HandControl.Width - padding
            Point(xCoord, horizTop)
        handControlMap[Seat.West].Location <-
            Point(padding, horizTop)

            // bid control
        bidControl.Location <-
            let handCtrl = handControlMap[Seat.South]
            new Point(
                handCtrl.Left + handCtrl.ClientSize.Width + 5,
                handCtrl.Top)

            // trick control
        let size, location =
            let innerLeft = handControlMap[Seat.West].Right
            let innerRight = handControlMap[Seat.East].Left
            let innerTop = handControlMap[Seat.North].Bottom
            let innerBottom = handControlMap[Seat.South].Top
            let innerWidth = innerRight - innerLeft
            let innerHeight = innerBottom - innerTop
            let size = Size(innerWidth, innerHeight) - 2 * Size(padding, padding)
            let location = Point(innerLeft, innerTop) + Size(padding, padding)
            size, location
        trickControl.Size <- size
        trickControl.Location <- location

            // go button
        goButton.Location <-
            let handCtrl = handControlMap[Seat.South]
            let size =
                Size(
                    (handCtrl.Size.Width - goButton.Size.Width) / 2,
                    handCtrl.Size.Height + 10)
            handCtrl.Location + size

            // score control
        scoreControl.Location <-
            let padding = 40
            Point(
                padding,
                this.ClientRectangle.Height - scoreControl.Height - padding)

    /// Queues an action that will disable the queue.
    let delayDisableQueue () =
        actionQueue.Enqueue(fun () ->
            goButton.Visible <- true
            actionQueue.Enabled <- false)

    /// Go button has been clicked. Resume handling events.
    let onGo _ =
        actionQueue.Enabled <- true
        goButton.Visible <- false

    /// A game has started.
    let onGameStart () =
        scoreControl.Score <- Score.zero

    /// A game has started.
    let delayGameStart args =
        actionQueue.Enqueue(fun () -> onGameStart args)

    /// A deal has started.
    let onDealStart (dealer, deal : OpenDeal) =
        for seat in Seat.cycle dealer do
            let handCtrl = handControlMap[seat]
            handCtrl.Cards <- deal.UnplayedCardMap[seat]

    /// A deal has started.
    let delayDealStart args =
        actionQueue.Enqueue(fun () -> onDealStart args)

    /// An auction has started.
    let onAuctionStart (leader : Seat) =
        trickControl.Leader <- leader

    /// An auction has started.
    let delayAuctionStart args =
        actionQueue.Enqueue(fun () -> onAuctionStart args)

    /// A player has bid.
    let onBid (seat, bid, _) =
        handControlMap[seat].Bid <- bid

    /// A player has bid.
    let delayBid args =
        actionQueue.Enqueue(fun () -> onBid args)

    /// An auction has finished.
    let onAuctionFinish () =
        ()

    /// An auction has finished.
    let delayAuctionFinish args =
        delayDisableQueue ()   // pause for Go button
        actionQueue.Enqueue(fun () -> onAuctionFinish args)

    /// A trick has started.
    let onTrickStart leader =
        trickControl.Leader <- leader

    /// A trick has started.
    let delayTrickStart args =
        actionQueue.Enqueue(fun () -> onTrickStart args)

    // A player has played a card.
    let onPlay (seat, card : Card, deal) =

        match deal.ClosedDeal.PlayoutOpt with
            | Some playout ->

                    // set trump on first play
                if playout.CompletedTricks.IsEmpty
                    && (Playout.currentTrick playout).Cards.Length = 1 then
                    for (KeyValue(_, ctrl)) in handControlMap do
                        ctrl.Trump <- card.Suit
                    trickControl.Trump <- card.Suit

                    // remove card from hand
                let ctrl = handControlMap[seat]
                ctrl.Remove(card)

                    // add card to trick
                trickControl.SetCard(seat, card)

            | None -> failwith "Unexpected"

    /// A player has played a card.
    let delayPlay args =
        actionQueue.Enqueue(fun () -> onPlay args)

    /// A trick has finished.
    let onTrickFinish () =
        trickControl.Clear()

    /// A trick has finished.
    let delayTrickFinish args =
        delayDisableQueue ()   // pause for Go button
        actionQueue.Enqueue(fun () -> onTrickFinish args)

    /// A deal has finished.
    let onDealFinish (_, _, gameScore) =
        scoreControl.Score <- gameScore

    /// A deal has finished.
    let delayDealFinish args =
        actionQueue.Enqueue(fun () -> onDealFinish args)

    /// A game has finished.
    let onGameFinish (_, gameScore) =
        Score.winningTeamOpt gameScore
            |> Option.iter (scoreControl.IncrementGamesWon)

    /// A game has finished.
    let delayGameFinish args =
        delayDisableQueue ()   // pause for Go button
        actionQueue.Enqueue(fun () -> onGameFinish args)

    /// User player
    let userPlayer =
        let handControl = handControlMap[Seat.South]
        User.player bidControl handControl actionQueue

    /// Underlying session.
    let session =
        let playerMap =
            Map [
                Seat.West, DeepCfr.Learn.Trickster.player
                Seat.North, DeepCfr.Learn.Trickster.player
                Seat.East, DeepCfr.Learn.Trickster.player
                Seat.South, userPlayer
            ]
        let rng = Random()
        Session(playerMap, rng, this)

    /// Runs the session once form is loaded.
    let onLoad _ =
        async { session.Run() }
            |> Async.Start

    do
            // layout controls
        this.Resize.Add(onResize)
        onResize ()

            // initialize handlers
        session.GameStartEvent.Add(delayGameStart)
        session.DealStartEvent.Add(delayDealStart)
        session.AuctionStartEvent.Add(delayAuctionStart)
        session.BidEvent.Add(delayBid)
        session.AuctionFinishEvent.Add(delayAuctionFinish)
        session.TrickStartEvent.Add(delayTrickStart)
        session.PlayEvent.Add(delayPlay)
        session.TrickFinishEvent.Add(delayTrickFinish)
        session.DealFinishEvent.Add(delayDealFinish)
        session.GameFinishEvent.Add(delayGameFinish)
        goButton.Click.Add(onGo)
        this.Load.Add(onLoad)
