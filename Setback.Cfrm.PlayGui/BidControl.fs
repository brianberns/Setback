namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open Setback

/// Control for obtaining a user's bid:
///    * A panel of buttons, one per possible bid
type BidControl() as this =
    inherit Panel(
        Size = Size(120, 120),
        Font = new Font("Calibri", 15f),
        BackColor = Color.Transparent,
        ForeColor = Color.White)

    /// Event triggered when a bid button is selected.
    let bidSelectedEvent = new Event<_>()

    /// One button per possible bid.
    let buttonMap =
        let xCoord, yCoord = 10, 4
        let height = 27
        PlayingCards.Enum.getValues<Bid>
            |> Seq.mapi (fun iButton bid ->
                let button =
                    new RadioButton(
                        Text = bid.ToString(),
                        Tag = bid,
                        Height = height,
                        Location =
                            Point(
                                xCoord,
                                yCoord + iButton * height))
                        |> Control.addTo this
                button.Click.Add(fun _ ->
                    bidSelectedEvent.Trigger bid)
                bid, button)
            |> Map

    /// Answers the bid associated with the given button.
    let getBid (btn : RadioButton) =
        btn.Tag :?> Bid

    /// Answers the button that represents the given bid.
    member __.GetBidButton(bid : Bid) =
        buttonMap[bid]

    /// A bid button has been selected.
    [<CLIEvent>]
    member __.BidSelectedEvent = bidSelectedEvent.Publish
