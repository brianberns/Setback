namespace Setback

open PlayingCards

/// Points taken by each team.
type Score =
    {
        ScoreMap : Map<Team, int>
    }

    /// Number of points taken by the given team.
    member score.Item
        with get(team) = score.ScoreMap[team]

    /// Adds two scores.
    static member (+) (scoreA : Score, scoreB : Score) =
        {
            ScoreMap =
                Enum.getValues<Team>
                    |> Seq.map (fun team ->
                        let sum = scoreA[team] + scoreB[team]
                        team, sum)
                    |> Map
        }

module Score =

    /// Initial score.
    let private zeroMap =
        Enum.getValues<Team>
            |> Seq.map (fun team -> team, 0)
            |> Map

    /// Initial score.
    let zero = { ScoreMap = zeroMap }

    /// Creates a score for the given team.
    let create team points =
        {
            ScoreMap = Map.add team points zeroMap
        }

    /// Display string.
    let toString score =

        let sb = new System.Text.StringBuilder()
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        for team in Enum.getValues<Team> do
            writeline (sprintf "%A: %d" team score.ScoreMap[team])

        sb.ToString()
