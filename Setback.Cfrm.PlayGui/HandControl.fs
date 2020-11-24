﻿namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Graphical representation of a hand of cards.
type HandPanel() as this =
    inherit Panel(
        Size = new Size(HandPanel.Width, HandPanel.Height))

    /// X-coord of the left edge of the card at the given index.
    static let left iCard =
        (iCard * CardControl.Width) + ((iCard + 1) * HandPanel.Padding)

    /// Card controls.
    let cardControls =
        [|
            for iControl = 0 to Setback.numCardsPerHand - 1 do
                new CardControl(
                    Left = left iControl,
                    Top = HandPanel.Padding)
                    |> Control.addTo this
        |]

    /// Padding between cards.
    static member Padding = 5

    /// Width of this control.
    static member Width = left Setback.numCardsPerHand

    /// Height of this control.
    static member Height = CardControl.Height + 2 * HandPanel.Padding

    /// Cards displayed by this control.
    member __.Cards
        with set(cards) =

                // clear all previous cards
            for ctrl in cardControls do
                ctrl.Clear()

                // display given cards
            let indexedCards =
                cards
                    |> Seq.sortByDescending (fun (card : Card) ->
                        card.Suit, card.Rank)
                    |> Seq.indexed
            for (iCard, card) in indexedCards do
                cardControls.[iCard].Card <-card

        and get() =
            cardControls
                |> Seq.choose (fun ctrl -> ctrl.CardOpt)

/// Graphical representation of a hand of cards:
///    * A panel displaying the cards.
///    * A text label displaying the seat/bid.
type HandControl(seat : Seat) as this =
    inherit Panel(
        Size = Size(HandControl.Width, HandControl.Height))

    /// Height of the text label.
    static let labelHeight = 30

    /// Card panel.
    let panel =
        new HandPanel()
            |> Control.addTo this

    /// Default text.
    let defaultText =
        seat.ToString()

    /// Text label.
    let label =
        new Label(
            Size =
                Size(
                    HandControl.Width - (2 * HandPanel.Padding),
                    labelHeight),
            Location =
                Point(HandPanel.Padding, panel.ClientSize.Height),
            Text = defaultText,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = labelHeight,
            Font = new Font("Calibri", 15.0f))
            |> Control.addTo this

    /// Width of this control.
    static member Width = HandPanel.Width

    /// Height of this control.
    static member Height =
        HandPanel.Height + HandPanel.Padding + labelHeight

    /// Seat represented by this control.
    member __.Seat = seat

    /// Cards displayed by this control.
    member __.Cards
        with set(cards) =
            panel.Cards <- cards
        and get() =
            panel.Cards
