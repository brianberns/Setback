namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

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

    do
            // initialize controls
        this.Controls.AddRange(handControls)

            // layout controls
        this.Resize.Add(fun _ -> onResize ())
        onResize ()
        this.Visible <- true
