namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

[<AutoOpen>]
module SeatExt =
    type Seat with

        /// The user's seat.
        static member User = Seat.South

        /// Indicates whether the given seat is played by the user.
        member seat.IsUser = (seat = Seat.User)

module Seat =

    /// Answers the index of the given seat relative to the given
    /// base seat.
    let getIndex (seat : Seat) (baseSeat : Seat) =
        let idx = ((int seat) - (int baseSeat) + Seat.numSeats) % Seat.numSeats
        assert(idx >= 0)
        assert(idx < Seat.numSeats)
        idx

module DealView =

    /// Creates card backs at the center of the given surface.
    let private getCardBacks surface =
        let pos =
            surface |> CardSurface.getPosition Point.origin
        Seq.init Card.numCards (fun _ ->
            let cardView = CardView.ofBack ()
            JQueryElement.setPosition pos cardView
            surface.Element.append(cardView)
            cardView)
            |> Seq.rev
            |> Seq.toArray

    /// Creates a closed hand view of the given batches of card
    /// backs.
    let private closedView backsA backsB =
        Seq.append backsA backsB
            |> ClosedHandView.ofCardViews

    /// Animates the start of the given deal on the given surface.
    let start surface dealer deal =

            // create closed hand views for dealing
        let backs = getCardBacks surface
        let closed1 = closedView backs.[0.. 2] backs.[12..14]
        let closed2 = closedView backs.[3.. 5] backs.[15..17]
        let closed3 = closedView backs.[6.. 8] backs.[18..20]
        let closed0 = closedView backs.[9..11] backs.[21..23]   // dealer receives cards last
        let closedHandViews =
            [| closed0; closed1; closed2; closed3 |]

            // create open hand view for user
        let iUser = Seat.getIndex Seat.User dealer
        let openHandView =
            deal.UnplayedCards.[iUser]
                |> OpenHandView.ofHand

            // deal animation
        let anim =

                // animate hands being dealt
            let seat iPlayer = Seat.incr iPlayer dealer
            let anim1a, anim1b = HandView.dealAnim surface (seat 1) closed1
            let anim2a, anim2b = HandView.dealAnim surface (seat 2) closed2
            let anim3a, anim3b = HandView.dealAnim surface (seat 3) closed3
            let anim0a, anim0b = HandView.dealAnim surface (seat 0) closed0

                // animate user's hand reveal
            let animReveal =
                let closedHandView = closedHandViews.[iUser]
                OpenHandView.revealAnim closedHandView openHandView

                // animate remaining deck removal
            let animRemove =
                backs.[24..]
                    |> Array.map (fun back ->
                        Animation.create back Remove)
                    |> Animation.Parallel

                // assemble complete animation
            [|
                anim1a; anim2a; anim3a; anim0a
                anim1b; anim2b; anim3b; anim0b
                animReveal; animRemove
            |] |> Animation.Serial 

        promise {

                // run the deal start animation
            do! Animation.run anim

                // answer the hand views for futher animation
            return closedHandViews
                |> Array.mapi (fun iPlayer closedHandView ->
                    let handView =
                        if iPlayer = iUser then openHandView
                        else closedHandView
                    let seat = Seat.incr iPlayer dealer
                    seat, handView)
        }
