﻿namespace Setback.Cfrm.PlayGui

open System.Drawing
open System.Windows.Forms

type ScoreControl() as this =
    inherit TableLayoutPanel(
        Size = Size(180, 75),
        ColumnCount = 3,
        RowCount = 2,
        Font = new Font("Calibri", 12.0f),
        ForeColor = Color.White,
        CellBorderStyle = TableLayoutPanelCellBorderStyle.Single)

        do
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