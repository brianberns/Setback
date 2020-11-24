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
        Size = Size(1100, 700))

    /// Unplayed cards for each seat.
    let handControlMap =
        Enum.getValues<Seat>
            |> Seq.map (fun seat ->
                let ctrl =
                    new HandControl(seat)
                        |> Control.addTo this
                ctrl.Cards <- Card.allCards |> Seq.take 6   // REMOVE
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
        let handCtrl = handControlMap.[Seat.South]
        bidControl.Location <-
            new Point(
                handCtrl.Left + handCtrl.ClientSize.Width + 5,
                handCtrl.Top)

            // trick control
        let innerLeft = handControlMap.[Seat.West].Right
        let innerRight = handControlMap.[Seat.East].Left
        let innerTop = handControlMap.[Seat.North].Bottom
        let innerBottom = handControlMap.[Seat.South].Top
        let innerWidth = innerRight - innerLeft
        let innerHeight = innerBottom - innerTop
        trickControl.Size <- Size(innerWidth, innerHeight) - 2 * Size(CardControl.Width, CardControl.Height)
        trickControl.Location <- Point(innerLeft, innerTop) + Size(CardControl.Width, CardControl.Height)

            // go button
        goButton.Location <-
            let handCtrl = handControlMap.[Seat.South]
            let size =
                Size(
                    (handCtrl.Size.Width - goButton.Size.Width) / 2,
                    handCtrl.Size.Height + 10)
            handCtrl.Location + size

        // a new deal has started
    let onDealStart (dealer, deal : AbstractOpenDeal) =
        let indexedSeats =
            dealer
                |> Seat.cycle
                |> Seq.indexed
        for (iPlayer, seat) in indexedSeats do
            let handCtrl = handControlMap.[seat]
            handCtrl.Cards <- deal.UnplayedCards.[iPlayer]

    let session =
        let dbPlayer =
            DatabasePlayer.player "Setback.db"
        let playerMap =
            Map [
                Seat.West, dbPlayer
                Seat.North, dbPlayer
                Seat.East, dbPlayer
            ]
        let userBid score openDeal =
            async {
                return dbPlayer.MakeBid score openDeal
            }
        let userPlay score openDeal =
            async {
                return dbPlayer.MakePlay score openDeal
            }
        let rng = Random(0)
        Session(playerMap, userBid, userPlay, rng)

    do
            // layout controls
        this.Resize.Add(fun _ -> onResize ())
        onResize ()

            // initialize handlers
        session.DealStartEvent.Add(onDealStart)

            // run
        try
            let iWinningTeam = session.Start()
            MessageBox.Show($"Winning team: {iWinningTeam}") |> ignore
        with ex ->
            MessageBox.Show($"{ex.Message}\r\n{ex.StackTrace.[0..400]}") |> ignore
