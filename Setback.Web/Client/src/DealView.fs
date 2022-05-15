namespace Setback.Web.Client

open PlayingCards
open Setback
open Setback.Cfrm

module DealView =

    let create surface deal =

            // create hand view
        let handView =
            HandView.create deal.UnplayedCards.[0]

            // create deal animation
        let dealAnim =

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

                // create animations of hands being dealt
            let handW1, handW2 = HandView.dealWest  surface backs.[0.. 2] backs.[12..14]
            let handN1, handN2 = HandView.dealNorth surface backs.[3.. 5] backs.[15..17]
            let handE1, handE2 = HandView.dealEast  surface backs.[6.. 8] backs.[18..20]
            let handS1, handS2 = HandView.dealSouth surface backs.[9..11] backs.[21..23]

                // create animation of south's hand reveal
            let finish =
                let reveal =
                    let southBacks =
                        Seq.append backs.[9..11] backs.[21..23]
                    HandView.reveal southBacks handView
                let remove =
                    seq {
                        for back in backs.[24..] do
                            yield Animation.create back Remove
                    } |> Animation.Parallel
                seq { reveal; remove } |> Animation.Serial

                // assemble final animation
            seq {
                handW1; handN1; handE1; handS1
                handW2; handN2; handE2; handS2
                finish
            } |> Animation.Serial 

        promise {

                // deal the cards
            do! Animation.run dealAnim

            return handView
        }
