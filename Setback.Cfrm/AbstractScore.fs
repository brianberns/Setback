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
        Array.replicate Team.numTeams 0
            |> AbstractScore

    /// Creates an abstract score from the given points, relative
    /// to the given team.
    let create baseTeam (points : Map<Team, int>) =
        baseTeam
            |> Team.cycle
            |> Seq.map (fun team -> points.[team])
            |> Seq.toArray
            |> AbstractScore

    /// The difference between "our" score and "their" score.
    let delta iTeam (score : AbstractScore) =
        assert(Team.numTeams = 2)
        assert(iTeam >= 0 && iTeam < Team.numTeams)
        score.[iTeam] - score.[1 - iTeam]
