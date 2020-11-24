namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Graphical representation of a trick of cards.
type TrickControl() as this =
    inherit Panel()

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
    let onResize () =
        let xCoord = (this.ClientSize.Width - CardControl.Width) / 2
        let yCoord = (this.ClientSize.Height - CardControl.Height) / 2
        cardControlMap.[Seat.West].Location <- Point(0, yCoord)
        cardControlMap.[Seat.North].Location <- Point(xCoord, 0)
        cardControlMap.[Seat.East].Location <-
            Point(this.ClientSize.Width - CardControl.Width, yCoord)
        cardControlMap.[Seat.South].Location <-
            Point(xCoord, this.ClientSize.Height - CardControl.Height)

        // layout controls
    do
        this.Resize.Add(fun _ -> onResize ())
        onResize ()
