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

    /// Go button has been clicked. Resume handling events.
    let onGo _ =
        actionQueue.Enabled <- true
        goButton.Visible <- false

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

    /// A trick has finished.
    let onTrickFinish () =
        trickControl.Clear()

    /// A trick has finished.
    let delayTrickFinish args =

            // disable handlers until Go button clicked
        actionQueue.Enqueue(fun () ->
            goButton.Visible <- true
            actionQueue.Enabled <- false)

            // then finish the trick
        actionQueue.Enqueue(fun () -> onTrickFinish args)

    /// A player has bid.
    let onBid (seat, bid, _) =
        handControlMap.[seat].Bid <- bid

    /// A player has bid.
    let delayBid args =
        actionQueue.Enqueue(fun () -> onBid args)

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
        session.DealStartEvent.Add(delayDealStart)
        session.TrickFinishEvent.Add(delayTrickFinish)
        session.BidEvent.Add(delayBid)
        session.PlayEvent.Add(delayPlay)
        goButton.Click.Add(onGo)

            // run
        async { session.Start() }
            |> Async.Start
