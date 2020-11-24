namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards
open Setback

/// Graphical representation of a hand of cards:
///    * A panel displaying the cards.
///    * A text label displaying the seat/bid.
type HandControl(seat : Seat) as this =
    inherit Panel(
        Size = new Size(HandControl.Width, HandControl.Height))

    /// Padding between cards.
    static let padding = 5

    /// Heigh of the card panel.
    static let panelHeight = CardControl.Height + 2 * padding

    /// Height of the text label.
    static let labelHeight = 30

    /// X-coord of the left edge of the card at the given index.
    static let left iCard =
        (iCard * CardControl.Width) + ((iCard + 1) * padding)

    /// Card panel.
    let panel =
        new Panel(
            Size = new Size(HandControl.Width, panelHeight))

    /// Default text.
    let defaultText =
        seat.ToString()

    /// Text label.
    let label =
        new Label(
            Size = new Size(HandControl.Width - (2 * padding), labelHeight),
            Location = new Point(padding, panel.ClientSize.Height),
            Text = defaultText,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = labelHeight,
            Font = new Font("Calibri", 15.0f))

        // initialize
    do
        this.Controls.Add(panel)
        this.Controls.Add(label)

    /// Width of this control.
    static member Width = left Setback.numCardsPerHand

    /// Height of this control.
    static member Height = panelHeight + labelHeight + padding

    /// Cards displayed by this control.
    member __.Cards
        with set(cards) =

                // remove existing cards from panel
            panel.Controls.Clear()

                // add given cards to panel
            cards
                |> Seq.sortByDescending (fun (card : Card) ->
                    card.Suit, card.Rank)
                |> Seq.mapi (fun iCard card ->
                    new CardControl(
                        card,
                        Left = left iCard,
                        Top = padding))
                |> panel.Controls.AddRange

                // reset to default label
            label.Text <- defaultText

    /// Contained card controls.
    member __.CardControls =
        panel.Controls
            |> Seq.cast<CardControl>

    /// Seat represented by this control.
    member __.Seat = seat
