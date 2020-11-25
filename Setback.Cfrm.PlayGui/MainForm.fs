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
        Size = Size(1100, 700),
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
            Visible = false)
            |> Control.addTo this

    /// Lays out controls.
    let onResize () =

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

    let actionQueue = ActionQueue(500)

    /// A new deal has started.
    let onDealStart (dealer, deal) =
        let indexedSeats =
            dealer
                |> Seat.cycle
                |> Seq.indexed
        for (iPlayer, seat) in indexedSeats do
            let handCtrl = handControlMap.[seat]
            handCtrl.Cards <- deal.UnplayedCards.[iPlayer]

    /// A new deal has started.
    let delayDealStart args =
        actionQueue.Enqueue(fun () -> onDealStart args)

    /// A player has bid.
    let onBid (seat, bid, _) =
        handControlMap.[seat].Bid <- bid

    /// A player has bid.
    let delayBid args =
        actionQueue.Enqueue(fun () -> onBid args)

    // A player has played a card.
    let onPlay (seat, card, _) =

            // remove card from hand
        let ctrl = handControlMap.[seat]
        ctrl.Clear(card)

    /// A player has played a card.
    let delayPlay args =
        actionQueue.Enqueue(fun () -> onPlay args)

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
        this.Resize.Add(fun _ -> onResize ())
        onResize ()

            // initialize handlers
        session.DealStartEvent.Add(delayDealStart)
        session.BidEvent.Add(delayBid)
        session.PlayEvent.Add(delayPlay)

            // run
        async { session.Start() }
            |> Async.Start
