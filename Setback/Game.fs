namespace Setback

module Game =

    /// Which team (if any) won the game with the given score.
    let winningTeamOpt score =
        let maxPoint = Array.max score.Points
        if maxPoint >= Setback.winThreshold then
            score.Points
                |> Seq.indexed
                |> Seq.where (fun (_, pt) ->
                    pt = maxPoint)
                |> Seq.map (fst >> enum<Team>)
                |> Seq.tryExactlyOne
        else None
