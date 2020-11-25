namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Graphical representation of a trick of cards.
type TrickControl() as this =
    inherit Panel(
        BackColor = Color.Transparent)

    /// One card per seat.
    let cardControlMap =
        Enum.getValues<Seat>
            |> Seq.map (fun seat ->
                let ctrl =
                    new CardControl()
                        |> Control.addTo this
                seat, ctrl)
            |> Map

    /// Lays out controls.
    let onResize _ =

        let padding = 30
        let xCoord = (this.ClientSize.Width - CardControl.Width) / 2
        let yCoord = (this.ClientSize.Height - CardControl.Height) / 2
        cardControlMap.[Seat.West].Location <- Point(padding, yCoord)
        cardControlMap.[Seat.North].Location <- Point(xCoord, padding)
        cardControlMap.[Seat.East].Location <-
            Point(this.ClientSize.Width - CardControl.Width - padding, yCoord)
        cardControlMap.[Seat.South].Location <-
            Point(xCoord, this.ClientSize.Height - CardControl.Height - padding)

        this.Invalidate()   // erase any previously drawn borders

    /// Draw a border.
    let onPaint (args : PaintEventArgs) =
        ControlPaint.DrawBorder(
            args.Graphics,
            this.ClientRectangle,
            Color.White,
            ButtonBorderStyle.Inset)

    /// Trump suit.
    let mutable trumpOpt = Option<Suit>.None

        // layout controls
    do
        this.Resize.Add(onResize)
        this.Paint.Add(onPaint)
        onResize ()

    /// Sets the card played by the given seat in this trick.
    member __.SetCard(seat, card) =
        let ctrl = cardControlMap.[seat]
        ctrl.Card <- card
        ctrl.IsTrump <- (Some card.Suit = trumpOpt)

    /// Clears the card played by the given seat in this trick.
    member __.ClearCard(seat) =
        cardControlMap.[seat].Clear()

    /// Clears all cards in this trick.
    member __.Clear() =
        let ctrls =
            cardControlMap
                |> Map.toSeq
                |> Seq.map snd
        for ctrl in ctrls do
            ctrl.Clear()

    /// Sets the trump suit.
    member __.Trump
        with set(trump) =
            trumpOpt <- Some trump

    /// Clears the trump suit.
    member __.ClearTrump() =
        trumpOpt <- None
