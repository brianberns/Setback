namespace Setback

open PlayingCards

/// Points taken by each team.
type Score =
    {
        /// Number of points taken by each team.
        Points : int[]
    }

    /// Number of points taken by the given team.
    member score.Item
        with get(team : Team) = score.Points[int team]

    /// Adds two scores.
    static member (+) (scoreA : Score, scoreB : Score) =
        {
            Points =
                Enum.getValues<Team>
                    |> Array.map (fun team ->
                        scoreA[team] + scoreB[team])
        }

module Score =

    /// Creates a score from the given per-team points.
    let ofPoints points =
        { Points = points }

    /// Initial score.
    let zero =
        Array.zeroCreate Team.numTeams
            |> ofPoints
