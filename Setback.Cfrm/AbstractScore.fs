namespace Setback.Cfrm

open Setback

/// Abstract view of a score, relative to a specific team.
type AbstractScore =
    | AbstractScore of int[]

    /// Answers score of the team with the given index relative to the
    /// base team.
    member this.Item(index) =
        let (AbstractScore points) = this
        points.[index]

    /// Adds two scores.
    static member (+) (AbstractScore pointsA, AbstractScore pointsB) =
        Array.map2 (+) pointsA pointsB
            |> AbstractScore

module AbstractScore =

    /// No score.
    let zero =
        Array.replicate Setback.numTeams 0
            |> AbstractScore

    /// The difference between "our" score and "their" score.
    let delta iTeam (score : AbstractScore) =
        assert(Setback.numTeams = 2)
        assert(iTeam >= 0 && iTeam < Setback.numTeams)
        score.[iTeam] - score.[1 - iTeam]
