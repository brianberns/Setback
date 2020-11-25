namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards

module Suit =

    /// Color of the given suit.
    let color = function
        | Suit.Spades
        | Suit.Clubs -> Color.Black
        | Suit.Hearts
        | Suit.Diamonds -> Color.DarkRed
        | _ -> failwith "Unknown suit"

module Card =

    /// String representation of the given card.
    let toAbbr (card : Card) =
        sprintf "%c%c" card.Rank.Char card.Suit.Char

module Control =

    /// Adds the given control to the given parent control fluently.
    let addTo (parent : Control) (control : 't when 't :> Control) =
        parent.Controls.Add(control)
        control

/// Graphical representation of a single card.
type CardControl() as this =
    inherit Label(
        Size = Size(CardControl.Width, CardControl.Height),
        Font = CardControl.GetFont(false),
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.White,
        Visible = false)

    /// Font used for non-trump cards.
    static let defaultFont =
        new Font("Lucida Console", 15.0f)

    /// Font used for trump cards.
    static let trumpFont =
        new Font(defaultFont, FontStyle.Underline)

    /// Card represented by this control, if any.
    let mutable cardOpt = Option<Card>.None

    /// Width of this control.
    static member Width = 36

    /// Height of this control.
    static member Height = 48

    /// Font to use.
    static member private GetFont(isTrump) =
        if isTrump then trumpFont
        else defaultFont

    /// Card represented by this control, if any.
    member __.CardOpt
        with get () = cardOpt

    /// Sets the card represented by this control.
    member __.Card
        with set(card) =
            cardOpt <- Some card
            this.Text <- Card.toAbbr card
            this.ForeColor <- Suit.color card.Suit
            this.Font <- defaultFont
            this.Visible <- true

    /// Clears this control.
    member __.Clear() =
        cardOpt <- None
        this.Text <- ""
        this.ForeColor <- Color.Transparent
        this.Font <- defaultFont
        this.Visible <- false

    /// Indicates whether the card represented by this control
    /// is trump.
    member __.IsTrump
        with set(isTrump) =
            this.Font <- CardControl.GetFont(isTrump)
