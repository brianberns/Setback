﻿namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Graphical representation of a hand of cards.
type HandPanel() as this =
    inherit Panel(
        Size = new Size(HandPanel.Width, HandPanel.Height),
        BackColor = Color.Transparent)

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

    /// This control's card controls.
    member _.CardControls =
        cardControls

    /// Cards displayed by this control.
    member _.Cards
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
                cardControls[iCard].Card <-card

        and get() =
            cardControls
                |> Seq.choose (fun ctrl -> ctrl.CardOpt)

    /// Sets trump suit.
    member _.Trump
        with set (trump : Suit) =
            for ctrl in cardControls do
                match ctrl.CardOpt with
                    | Some card ->
                        ctrl.IsTrump <- card.Suit = trump
                    | None -> ()

    /// Removes the given card from this control.
    member _.Remove(card) =
        let cardControl =
            cardControls
                |> Seq.find (fun ctrl ->
                    ctrl.CardOpt = Some card)
        cardControl.Clear()

/// Graphical representation of a hand of cards:
///    * A panel displaying the cards.
///    * A text label displaying the seat/bid.
type HandControl(seat : Seat) as this =
    inherit Panel(
        Size = Size(HandControl.Width, HandControl.Height),
        BackColor = Color.Transparent)

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
            Font = new Font("Calibri", 15f),
            ForeColor = Color.White)
            |> Control.addTo this

    /// A card has been selected.
    let cardSelectedEvent = Event<_>()

    do
            // prepare card selection event
        for ctrl in panel.CardControls do
            ctrl.Click.Add(fun _ ->
                ctrl.CardOpt
                    |> Option.iter (fun card ->
                        cardSelectedEvent.Trigger(card)))

    /// Width of this control.
    static member Width = HandPanel.Width

    /// Height of this control.
    static member Height =
        HandPanel.Height + HandPanel.Padding + labelHeight

    /// Seat represented by this control.
    member _.Seat = seat

    /// This control's card controls.
    member _.CardControls =
        panel.CardControls

    /// Cards displayed by this control.
    member _.Cards
        with set(cards) =
            panel.Cards <- cards
            label.Text <- defaultText
        and get() =
            panel.Cards

    /// Bid displayed by this control.
    member _.Bid
        with set (bid : Bid) =
            label.Text <- $"{seat}: {bid}"

    /// Sets trump suit.
    member _.Trump
        with set(trump : Suit) =
            panel.Trump <- trump

    /// Removes the given card from this control.
    member _.Remove(card) =
        panel.Remove(card)

    /// Indicates whether to show card fronts or backs.
    member _.ShowFront
        with set(value : bool) =
            for ctrl in panel.CardControls do
                ctrl.ShowFront <- value

    [<CLIEvent>]
    member _.CardSelectedEvent = cardSelectedEvent.Publish
