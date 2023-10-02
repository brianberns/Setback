namespace Setback.Cfrm

open Setback

/// Abstract view of a score, relative to a specific team.
//  To-do: rename this type, since it is sometimes used as an absolute score.
type AbstractScore =
    | AbstractScore of int[]

    /// Answers score of the team with the given index relative to the
    /// base team.
    member this.Item(teamIdx) =
        let (AbstractScore points) = this
        points.[teamIdx]

    /// Adds two scores.
    static member (+) (AbstractScore pointsA, AbstractScore pointsB) =
        Array.map2 (+) pointsA pointsB
            |> AbstractScore

module AbstractScore =

    /// No score.
    let zero =
        Array.replicate Setback.numTeams 0
            |> AbstractScore

    /// Creates a score with the given value for the given team,
    /// and zero for other teams.
    let forTeam teamIdx value =
        assert(teamIdx >= 0 && teamIdx < Setback.numTeams)
        Array.init Setback.numTeams (fun iTeam ->
            if iTeam = teamIdx then value
            else 0)
            |> AbstractScore

    /// Shifts the given score so it is relative to the given
    /// team.
    let shift teamIdx (AbstractScore points) =
        AbstractScore [|
            for iTeam = 0 to points.Length - 1 do
                let iShift =
                    (teamIdx + iTeam) % points.Length
                yield points.[iShift]
        |]

    /// The difference between "our" score and "their" score.
    let delta iTeam (score : AbstractScore) =
        assert(Setback.numTeams = 2)
        assert(iTeam >= 0 && iTeam < Setback.numTeams)
        score.[iTeam] - score.[1 - iTeam]
