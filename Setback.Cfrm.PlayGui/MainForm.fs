namespace Setback.Cfrm.PlayGui

open System
open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback
open Setback.Cfrm

/// Main form for playing Setback.
type MainForm() as this =
    inherit Form(
        Text = "Bernsrite Setback",
        Size = new Size(1100, 700))

    /// Unplayed cards for each seat.
    let handControls =
        Enum.getValues<Seat>
            |> Array.map (fun seat ->
                new HandControl(seat))

    /// Unplayed cards for each seat.
    let handControlMap =
        handControls
            |> Seq.map (fun ctrl ->
                ctrl.Cards <- Card.allCards |> Seq.take 6
                ctrl.Seat, ctrl)
            |> Map

    /// User's bid control.
    let bidControl =
        new BidControl(
            Height = HandControl.Height,
            Visible = false)

    /// User must click to continue.
    let goButton =
        new Button(
            Size = Size(100, 30),
            Text = "Go",
            Font = new Font("Calibri", 12.0f),
            Visible = false)

    /// Lays out controls.
    let onResize () =

        let padding = 50

            // layout hand controls
        let vertLeft = (this.ClientSize.Width - HandControl.Width) / 2
        let horizTop = (this.ClientSize.Height - HandControl.Height - goButton.Height) / 2
        handControlMap.[Seat.North].Location <- new Point(vertLeft, padding)
        handControlMap.[Seat.South].Location <- new Point(vertLeft, this.ClientSize.Height - HandControl.Height - goButton.Height - padding)
        handControlMap.[Seat.East].Location <- new Point(this.ClientSize.Width - HandControl.Width - padding, horizTop)
        handControlMap.[Seat.West].Location <- new Point(padding, horizTop)

            // layout go button
        goButton.Location <-
            let handCtrl = handControlMap.[Seat.South]
            handCtrl.Location + Size((handCtrl.Size.Width - goButton.Size.Width)/ 2, handCtrl.Size.Height + 10)

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
            // initialize controls
        this.Controls.AddRange(handControls)

            // layout controls
        this.Resize.Add(fun _ -> onResize ())
        onResize ()
        this.Visible <- true

            // initialize handlers
        session.DealStartEvent.Add(onDealStart)

            // run
        try
            let iWinningTeam = session.Start()
            MessageBox.Show($"Winning team: {iWinningTeam}") |> ignore
        with ex ->
            MessageBox.Show($"{ex.Message}\r\n{ex.StackTrace.[0..400]}") |> ignore
