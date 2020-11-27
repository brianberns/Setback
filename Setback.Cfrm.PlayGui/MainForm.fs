namespace Setback.Cfrm.PlayGui

open System
open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback
open Setback.Cfrm

/// Main form for playing Setback:
///    * One hand control per seat, arranged roughly in a circle
///    * A bid control next to the users's hand control
///    * The user's "Go" button for advancing the game
type MainForm() as this =
    inherit Form(
        Text = "Bernsrite Setback",
        Size = Size(960, 730),
        BackColor = Color.DarkGreen)

    /// Unplayed cards for each seat.
    let handControlMap =
        Enum.getValues<Seat>
            |> Seq.map (fun seat ->
                let ctrl =
                    new HandControl(seat)
                        |> Control.addTo this
                seat, ctrl)
            |> Map

    /// User's bid control.
    let bidControl =
        new BidControl(
            Visible = true)
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
            Font = new Font("Calibri", 12.0f),
            UseVisualStyleBackColor = true,
            Visible = false)
            |> Control.addTo this

    /// Team scoreboard.
    let scoreControl =
        new ScoreControl()
            |> Control.addTo this

    /// Action queue for delaying event handlers.
    let actionQueue = ActionQueue(250)

    /// Lays out controls.
    let onResize _ =

        let padding = 50

            // hand controls
        let vertLeft =
            (this.ClientSize.Width - HandControl.Width) / 2
        let horizTop =
            (this.ClientSize.Height - HandControl.Height - goButton.Height) / 2
        handControlMap.[Seat.North].Location <-
            Point(vertLeft, padding)
        handControlMap.[Seat.South].Location <-
            let yCoord =
                this.ClientSize.Height - HandControl.Height - goButton.Height - padding
            Point(vertLeft, yCoord)
        handControlMap.[Seat.East].Location <-
            let xCoord =
                this.ClientSize.Width - HandControl.Width - padding
            Point(xCoord, horizTop)
        handControlMap.[Seat.West].Location <-
            Point(padding, horizTop)

            // bid control
        bidControl.Location <-
            let handCtrl = handControlMap.[Seat.South]
            new Point(
                handCtrl.Left + handCtrl.ClientSize.Width + 5,
                handCtrl.Top)

            // trick control
        let size, location =
            let innerLeft = handControlMap.[Seat.West].Right
            let innerRight = handControlMap.[Seat.East].Left
            let innerTop = handControlMap.[Seat.North].Bottom
            let innerBottom = handControlMap.[Seat.South].Top
            let innerWidth = innerRight - innerLeft
            let innerHeight = innerBottom - innerTop
            let size = Size(innerWidth, innerHeight) - 2 * Size(CardControl.Width, CardControl.Height)
            let location = Point(innerLeft, innerTop) + Size(CardControl.Width, CardControl.Height)
            size, location
        trickControl.Size <- size
        trickControl.Location <- location

            // go button
        goButton.Location <-
            let handCtrl = handControlMap.[Seat.South]
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
        scoreControl.Score <- AbstractScore.zero

    /// A game has started.
    let delayGameStart args =
        actionQueue.Enqueue(fun () -> onGameStart args)

    /// A deal has started.
    let onDealStart (dealer, deal) =
        let indexedSeats =
            dealer
                |> Seat.cycle
                |> Seq.indexed
        for (iPlayer, seat) in indexedSeats do
            let handCtrl = handControlMap.[seat]
            handCtrl.Cards <- deal.UnplayedCards.[iPlayer]

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
        handControlMap.[seat].Bid <- bid

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
                if playout.History.NumTricksCompleted = 0
                    && playout.CurrentTrick.NumPlays = 1 then
                    for (KeyValue(_, ctrl)) in handControlMap do
                        ctrl.Trump <- card.Suit
                    trickControl.Trump <- card.Suit

                    // remove card from hand
                let ctrl = handControlMap.[seat]
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

    /// Shifts from dealer-relative to absolute score.
    let shiftScore dealer score =
        let iDealerTeam =
            int dealer % Setback.numTeams
        let iAbsoluteTeam =
            (Setback.numTeams - iDealerTeam) % Setback.numTeams
        score |> AbstractScore.shift iAbsoluteTeam

    /// A deal has finished.
    let onDealFinish (dealer, _, gameScore) =
        scoreControl.Score <- shiftScore dealer gameScore

    /// A deal has finished.
    let delayDealFinish args =
        actionQueue.Enqueue(fun () -> onDealFinish args)

    /// A game has finished.
    let onGameFinish (dealer, gameScore) =
        gameScore
            |> shiftScore dealer
            |> BootstrapGameState.winningTeamOpt
            |> Option.get
            |> scoreControl.IncrementGamesWon

    /// A game has finished.
    let delayGameFinish args =
        actionQueue.Enqueue(fun () -> onGameFinish args)

    /// Underlying session.
    let session =
        let dbPlayer =
            DatabasePlayer.player "Setback.db"
        let playerMap =
            Map [
                Seat.West, dbPlayer
                Seat.North, dbPlayer
                Seat.East, dbPlayer
                Seat.South, dbPlayer
            ]
        let rng = Random(0)
        Session(playerMap, rng, this)

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

            // run
        async { session.Start() }
            |> Async.Start
