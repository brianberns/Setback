namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards

module Card =

    /// String representation of the given card.
    let toAbbr (card : Card) =
        sprintf "%c%c" card.Rank.Char card.Suit.Letter

module Control =

    /// Adds the given control to the given parent control fluently.
    let addTo (parent : Control) (control : 't when 't :> Control) =
        parent.Controls.Add(control)
        control

/// Graphical representation of a single card.
type CardControl() as this =
    inherit PictureBox(
        Size = Size(CardControl.Width, CardControl.Height),
        SizeMode = PictureBoxSizeMode.StretchImage,
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
    static member Width = 69

    /// Height of this control.
    static member Height = 106

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
            let assembly = System.Reflection.Assembly.GetExecutingAssembly()
            let name = $"{this.GetType().Namespace}.Images.{Card.toAbbr card}.png"
            use stream = assembly.GetManifestResourceStream(name)
            cardOpt <- Some card
            this.Image <- Image.FromStream(stream)
            this.Text <- Card.toAbbr card
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

    /// Indicates whether this control is clickable.
    member val IsClickable =
        false
        with set, get

    /// Allow click?
    override this.OnClick(e) =
        if this.IsClickable then
            base.OnClick(e)
