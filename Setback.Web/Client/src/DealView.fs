namespace Setback.Web.Client

open Setback
open Setback.Cfrm

module DealView =

    let create surface deal =

        let backs =
            let pos = surface |> CardSurface.getPosition (0.0, 0.0)
            Seq.init 52 (fun _ ->
                let cv = CardView.ofBack ()
                JQueryElement.setPosition pos cv
                surface.Element.append(cv)
                cv)
                |> Seq.rev
                |> Seq.toArray

        let stepW1, stepW2 = HandView.dealWest  surface backs.[0.. 2] backs.[12..14]
        let stepN1, stepN2 = HandView.dealNorth surface backs.[3.. 5] backs.[15..17]
        let stepE1, stepE2 = HandView.dealEast  surface backs.[6.. 8] backs.[18..20]
        let stepS1, stepS2 = HandView.dealSouth surface backs.[9..11] backs.[21..23]

        let handView =
            HandView.create deal.UnplayedCards.[0]
        let playAnim = handView |> HandView.play surface
        for cardView in handView do
            cardView.click(fun () ->
                playAnim cardView
                    |> Animation.run
                    |> ignore)

        let finish =
            let southBacks =
                Seq.append backs.[9..11] backs.[21..23]
            let reveal =
                HandView.reveal southBacks handView
            let remove =
                seq {
                    for back in backs.[24..] do
                        yield Animation.create back Remove
                } |> Animation.Parallel
            Animation.Serial [ reveal; remove ]

        seq {
            stepW1; stepN1; stepE1; stepS1
            stepW2; stepN2; stepE2; stepS2
            finish
        }
            |> Animation.Serial 
            |> Animation.run
