namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

module DealView =

    /// Animates the start of the given deal on the given surface.
    let start surface deal =

            // create card backs
        let backs =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init Card.numCards (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

            // create closed hand views for dealing
        let closedView backs1 backs2 =
            Array.append backs1 backs2
                |> ClosedHandView.ofCardViews
        let closedW = closedView backs.[0.. 2] backs.[12..14]
        let closedN = closedView backs.[3.. 5] backs.[15..17]
        let closedE = closedView backs.[6.. 8] backs.[18..20]
        let closedS = closedView backs.[9..11] backs.[21..23]

            // create open hand view for human player
        let openS =
            OpenHandView.ofHand deal.UnplayedCards.[0]

            // deal animation
        let anim =

                // animate hands being dealt
            let animW1, animW2 = HandView.deal surface Seat.West  closedW
            let animN1, animN2 = HandView.deal surface Seat.North closedN
            let animE1, animE2 = HandView.deal surface Seat.East  closedE
            let animS1, animS2 = HandView.deal surface Seat.South closedS

                // animate south's hand reveal
            let reveal = OpenHandView.reveal closedS openS

                // animate remaining deck removal
            let remove =
                backs.[24..]
                    |> Array.map (fun back ->
                        Animation.create back Remove)
                    |> Animation.Parallel

                // assemble final animation
            [|
                animW1; animN1; animE1; animS1
                animW2; animN2; animE2; animS2
                reveal; remove
            |] |> Animation.Serial 

        promise {

                // run the animation
            do! Animation.run anim

                // answer the hand views for futher animation
            return [|
                Seat.West, closedW
                Seat.North, closedN
                Seat.East, closedE
                Seat.South, openS
            |]
        }
