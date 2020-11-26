namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

open Setback
open Setback.Cfrm

type ScoreControl() as this =
    inherit TableLayoutPanel(
        Size = Size(180, 75),
        ColumnCount = 3,
        RowCount = 2,
        Font = new Font("Calibri", 12.0f),
        ForeColor = Color.White,
        CellBorderStyle = TableLayoutPanelCellBorderStyle.Single)

    let createLabels iColumn =
        Array.init Setback.numTeams (fun iTeam ->
            let label = new Label()
            this.Controls.Add(label, iColumn, iTeam + 1)
            label)

    let scoreLabels = createLabels 1

    let gamesWonLabels = createLabels 2

    let displayScore (AbstractScore points) labels =
        (points, labels)
            ||> Array.iter2 (fun point (label : Label) ->
                label.Text <- point.ToString())

    let mutable gamesWon = AbstractScore.zero

    do
            // initialize columns
        let colDescs =
            [|
                "Team", 33.3f
                "Score", 33.3f
                "Games", 33.3f
            |] |> Seq.indexed
        for iColumn, (text, width) in colDescs do
            let label =
                new Label(
                    Text = text,
                    Font = new Font(this.Font, FontStyle.Bold),
                    Dock = DockStyle.Fill)
            this.Controls.Add(label, iColumn, 0)
            ColumnStyle(SizeType.Percent, width)
                |> this.ColumnStyles.Add
                |> ignore

        this.Controls.Add(new Label(Text = "N+S"), 0, 1)
        this.Controls.Add(new Label(Text = "E+W"), 0, 2)

            // initialize scores
        displayScore AbstractScore.zero scoreLabels
        displayScore gamesWon gamesWonLabels

    /// Score of current game.
    member __.Score
        with set(score) =
            displayScore score scoreLabels

    /// The given team won a game.
    member __.IncrementGamesWon(iTeam) =
        gamesWon <- gamesWon + AbstractScore.forTeam iTeam 1
        displayScore gamesWon gamesWonLabels
