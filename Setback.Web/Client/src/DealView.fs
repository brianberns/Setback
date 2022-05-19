namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

module DealView =

    /// Animates the start of the given deal on the given surface.
    let start surface dealer deal =

            // create card backs
        let backs =
            let pos = surface |> CardSurface.getPosition Point.origin
            Seq.init Card.numCards (fun _ ->
                let cardView = CardView.ofBack ()
                JQueryElement.setPosition pos cardView
                surface.Element.append(cardView)
                cardView)
                |> Seq.rev
                |> Seq.toArray

            // create closed hand views for dealing
        let closedView backs1 backs2 =
            Array.append backs1 backs2
                |> ClosedHandView.ofCardViews
        let closed1 = closedView backs.[0.. 2] backs.[12..14]
        let closed2 = closedView backs.[3.. 5] backs.[15..17]
        let closed3 = closedView backs.[6.. 8] backs.[18..20]
        let closed0 = closedView backs.[9..11] backs.[21..23]   // dealer receives cards last

            // create open hand view for user
        let openS =
            let cards = deal.UnplayedCards.[0]
            OpenHandView.ofHand cards

            // deal animation
        let anim =

                // animate hands being dealt
            let anim1a, anim1b = HandView.deal surface Seat.West  closed1
            let anim2a, anim2b = HandView.deal surface Seat.North closed2
            let anim3a, anim3b = HandView.deal surface Seat.East  closed3
            let anim0a, anim0b = HandView.deal surface Seat.South closed0

                // animate south's hand reveal
            let reveal = OpenHandView.reveal closed0 openS

                // animate remaining deck removal
            let remove =
                backs.[24..]
                    |> Array.map (fun back ->
                        Animation.create back Remove)
                    |> Animation.Parallel

                // assemble final animation
            [|
                anim1a; anim2a; anim3a; anim0a
                anim1b; anim2b; anim3b; anim0b
                reveal; remove
            |] |> Animation.Serial 

        promise {

                // run the initial animation
            do! Animation.run anim

                // answer the hand views for futher animation
            return [|
                0, openS
                1, closed1
                2, closed2
                3, closed3
            |] |> Array.map (fun (iPlayer, handView) ->
                Seat.incr iPlayer dealer, handView)
        }
