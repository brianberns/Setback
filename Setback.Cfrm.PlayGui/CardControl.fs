namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Reflection
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

    /// Card represented by this control, if any.
    let mutable cardOpt = Option<Card>.None

    /// Sets the control's current image.
    let setImage image =
        let oldImage = this.Image
        this.Image <- image
        if oldImage <> null then
            oldImage.Dispose()

    /// Card is trump?
    let mutable isTrump = false

    /// Show card front or back?
    let mutable showFront = true

    /// Draw a border for trump.
    let onPaint (args : PaintEventArgs) =
        if isTrump && showFront then
            let color = Color.Black
            let width = 2
            let style = ButtonBorderStyle.Solid
            ControlPaint.DrawBorder(
                args.Graphics,
                this.ClientRectangle,
                color, width, style,
                color, width, style,
                color, width, style,
                color, width, style)

    do
            // initialize handlers
        this.Paint.Add(onPaint)

    /// Width of this control.
    static member Width = 69

    /// Height of this control.
    static member Height = 106

    /// Card represented by this control, if any.
    member __.CardOpt
        with get () = cardOpt

    /// Sets the card represented by this control.
    member __.Card
        with set(card) =
            cardOpt <- Some card
            use stream =
                let assembly = Assembly.GetExecutingAssembly()
                let imageName =
                    if showFront then Card.toAbbr card
                    else "blue_back"
                $"{this.GetType().Namespace}.Images.{imageName}.png"
                    |> assembly.GetManifestResourceStream
            setImage <| Image.FromStream(stream)
            this.Visible <- true

    /// Clears this control.
    member __.Clear() =
        cardOpt <- None
        this.Visible <- false
        setImage null

    /// Indicates whether the card represented by this control
    /// is trump.
    member __.IsTrump
        with set(value) =
            isTrump <- value
            this.Invalidate()

    /// Indicates whether to show the card front or back.
    member __.ShowFront
        with set(value) =
            showFront <- value
            this.Invalidate()

    /// Indicates whether this control is clickable.
    member val IsClickable =
        false
        with set, get

    /// Allow click?
    override this.OnClick(e) =
        if this.IsClickable then
            base.OnClick(e)
