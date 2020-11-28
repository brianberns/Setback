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
        Font = new Font("Calibri", 12f),
        ForeColor = Color.White,
        CellBorderStyle = TableLayoutPanelCellBorderStyle.Single)

    let createLabels iColumn =
        Array.init Setback.numTeams (fun iTeam ->
            let label =
                new Label(
                    TextAlign = ContentAlignment.MiddleCenter,
                    Anchor = AnchorStyles.None)
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
                "Team", 33.3f, AnchorStyles.Left, ContentAlignment.MiddleLeft
                "Score", 33.3f, AnchorStyles.None, ContentAlignment.MiddleCenter
                "Games", 33.3f, AnchorStyles.None, ContentAlignment.MiddleCenter
            |] |> Seq.indexed
        for iColumn, (text, width, anchor, align) in colDescs do

                // create label
            let label =
                new Label(
                    Text = text,
                    Font = new Font(this.Font, FontStyle.Bold),
                    TextAlign = align,
                    Anchor = anchor)
            this.Controls.Add(label, iColumn, 0)

                // set column width
            ColumnStyle(SizeType.Percent, width)
                |> this.ColumnStyles.Add
                |> ignore

            // initialize rows
        assert(int Seat.East % Setback.numTeams = 0)
        for iRow, text in [| "E+W"; "N+S" |] |> Seq.indexed do
            let label =
                new Label(
                    Text = text,
                    TextAlign = ContentAlignment.MiddleLeft)
            this.Controls.Add(label, 0, iRow + 1)

            // initialize scores
        displayScore AbstractScore.zero scoreLabels
        displayScore gamesWon gamesWonLabels

    /// Score of current game (absolute index).
    member __.Score
        with set(score) =
            displayScore score scoreLabels

    /// The given team won a game (absolute index).
    member __.IncrementGamesWon(iTeam) =
        gamesWon <- gamesWon + AbstractScore.forTeam iTeam 1
        displayScore gamesWon gamesWonLabels
