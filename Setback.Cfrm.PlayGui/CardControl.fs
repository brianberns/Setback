namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open PlayingCards

[<AutoOpen>]
module AutoOpen =

    type Control.ControlCollection with

        /// Adds the given controls to the collection.
        member this.AddRange<'t when 't :> Control>(controls : seq<'t>) =
            controls
                |> Seq.cast<Control>
                |> Seq.toArray
                |> this.AddRange

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

/// Graphical representation of a single card.
type CardControl(card : Card) =
    inherit Label(
        Text = Card.toAbbr card,
        Font = new Font("Lucida Console", 15.0f),
        Size = new Size(CardControl.Width, CardControl.Height),
        BorderStyle = BorderStyle.FixedSingle,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.White,
        ForeColor = Suit.color card.Suit)

    /// Width of this control.
    static member Width = 36

    /// Height of this control.
    static member Height = 48

    /// Card represented by this control.
    member __.Card = card

    /// Indicates whether this control's card is trump.
    member this.IsTrump
        with set (isTrump) =
            let style =
                if isTrump then FontStyle.Underline
                else FontStyle.Regular
            this.Font <- new Font(this.Font, style)
