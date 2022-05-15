namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

module DealView =

    let create surface deal =

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

            // create open hand view
        let openS =
            OpenHandView.create deal.UnplayedCards.[0]

            // create deal animation
        let anim =

                // create animations of hands being dealt
            let animW1, animW2 = HandView.dealW surface closedW
            let animN1, animN2 = HandView.dealN surface closedN
            let animE1, animE2 = HandView.dealE surface closedE
            let animS1, animS2 = HandView.dealS surface closedS

                // create animation of south's hand reveal
            let finish =
                let reveal = OpenHandView.reveal closedS openS
                let remove =
                    seq {
                        for back in backs.[24..] do
                            yield Animation.create back Remove
                    } |> Animation.Parallel
                seq { reveal; remove } |> Animation.Serial

                // assemble final animation
            seq {
                animW1; animN1; animE1; animS1
                animW2; animN2; animE2; animS2
                finish
            } |> Animation.Serial 

        promise {

                // deal the cards
            do! Animation.run anim

            return openS
        }
