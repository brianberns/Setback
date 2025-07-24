namespace Setback

open System.Collections.Immutable

/// Points scored by each team during one or more deals. This can
/// be used to track both game and match points.
type Score =
    {
        Points : ImmutableArray<int>
    }
    member this.Item
        with get(team : Team) = this.Points.[team.Number]

module Score =

    /// Creates an all-zero score for the given teams.
    let zeroCreate teams =
        { Points = Team.zeroArray teams }

    /// Adds the given scores together.
    let add scoreA scoreB =
        let points =
            Seq.zip scoreA.Points scoreB.Points
                |> Seq.map (fun (pointA, pointB) -> pointA + pointB)
                |> Seq.toArray
        { Points = ImmutableArray.Create<int>(points) }

    /// Answers a new score with the given points for the given team
    let withPoints team points score =
        { Points = score.Points.SetItem(team.Number, points) }

    /// Computes score delta from a team's point of view
    let getDelta team score =
        score.Points
            |> Seq.mapi (fun teamNum points -> (teamNum, points))
            |> Seq.sumBy (fun (teamNum, points) ->
                if teamNum = team.Number then points
                else -points)

[<AutoOpen>]
module ScoreExt =
    type Score with
        member score.WithPoints(team, value) =
            score |> Score.withPoints team value
