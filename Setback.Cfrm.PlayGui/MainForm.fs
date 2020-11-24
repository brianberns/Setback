namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

type MainForm() as this =
    inherit Form(
        Text = "Bernsrite Setback",
        Size = new Size(1100, 700))

    let handCtrl =
        new HandControl(Seat.South)

    do
        handCtrl.Cards <- Card.allCards |> Seq.take Setback.numCardsPerHand
        this.Controls.Add(handCtrl)
