namespace Setback.DeepCfr.PlayGui

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
/// http://acbl.mybigcommerce.com/52-playing-cards/
type CardControl() as this =
    inherit Panel(
        Size = Size(CardControl.Width, CardControl.Height),
        BackColor = Color.Transparent,
        Visible = false)

    /// Card represented by this control, if any.
    let mutable cardOpt = Option<Card>.None

    /// Gets resource image with the given name.
    let getImage name =
        use stream =
            let assembly = Assembly.GetExecutingAssembly()
            $"{this.GetType().Namespace}.Images.{name}.png"
                |> assembly.GetManifestResourceStream
        Image.FromStream(stream)

    /// Picture of front of card.
    let frontPicture =
        new PictureBox(
            Size = this.Size,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Visible = true)
            |> Control.addTo this

    /// Trump indicator overlay.
    let trumpBadge =
        new PictureBox(
            Size = this.Size,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Visible = true)
            |> Control.addTo frontPicture

    /// Picture of back of card.
    let backPicture =
        new PictureBox(
            Size = this.Size,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = getImage "Back",
            Visible = false)
            |> Control.addTo this

    /// Sets the image of a picture box.
    let assignImage (pictureBox : PictureBox) image =
        let oldImage = pictureBox.Image
        pictureBox.Image <- image
        if oldImage <> null then
            oldImage.Dispose()

    do
        frontPicture.Click.Add(this.TriggerClick)
        trumpBadge.Click.Add(this.TriggerClick)
        backPicture.Click.Add(this.TriggerClick)

    /// Width of this control.
    static member Width = 69

    /// Height of this control.
    static member Height = 106

    /// Card represented by this control, if any.
    member _.CardOpt
        with get () = cardOpt

    /// Sets the card represented by this control.
    member _.Card
        with set(card) =

                // remember card
            cardOpt <- Some card

                // assign corresponding image
            card
                |> Card.toAbbr
                |> getImage
                |> assignImage frontPicture

                // show control
            this.Visible <- true

    /// Clears the card displayed by this control.
    member _.Clear() =

            // forget card
        cardOpt <- None

            // clear corresponding images
        assignImage frontPicture null
        assignImage trumpBadge null

            // hide pictures
        this.Visible <- false

    /// Indicates whether the card represented by this control
    /// is trump.
    member _.IsTrump
        with set(value) =
            let image =
                if value then getImage "Trump"
                else null
            image |> assignImage trumpBadge

    /// Indicates whether to show the card front or back.
    member _.ShowFront
        with set(value) =
            frontPicture.Visible <- value
            trumpBadge.Visible <- value
            backPicture.Visible <- not value

    /// Indicates whether this control is clickable.
    member val IsClickable =
        false
        with set, get

    /// Allow click?
    override _.OnClick(args) =
        if this.IsClickable then
            base.OnClick(args)

    /// Allows access to protected override.
    member private _.TriggerClick(args) =
        this.OnClick(args)
