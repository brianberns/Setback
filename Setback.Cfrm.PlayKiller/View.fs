namespace Setback

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout

module View =
 
    // Function to calculate the 95% confidence interval for a win rate
    let calculateConfidenceInterval (wins: int) (totalGames: int) =
        if totalGames = 0 then
            (0.0, 0.0)
        else
            let p = float wins / float totalGames
            let n = float totalGames
            let z = 1.96 // Z-score for 95% confidence
            let marginOfError = z * sqrt (p * (1.0 - p) / n)
            let lowerBound = p - marginOfError
            let upperBound = p + marginOfError
            (lowerBound, upperBound)

    // A reusable view for displaying a team's stats within a dynamic, zoomed-in range
    let teamStatsView (name: string) (wins: int) (totalGames: int) (viewMin: float) (viewMax: float) =
        let p = if totalGames = 0 then 0.5 else float wins / float totalGames
        let lower, upper = calculateConfidenceInterval wins totalGames
        let winRatePercent = p * 100.0

        let viewSpan = viewMax - viewMin
        // Avoid division by zero if the view has no span
        if viewSpan <= 0.0 then
            // If there's no range to display, show nothing
            DockPanel.create []
        else
            // Normalize the CI and win rate to the dynamic view range [viewMin, viewMax]
            let lower_norm = (max viewMin lower - viewMin) / viewSpan
            let upper_norm = (min viewMax upper - viewMin) / viewSpan
            let p_norm = (p - viewMin) / viewSpan

            let colDefs =
                let defs = ColumnDefinitions()
                // Column for the space before the CI bar
                defs.Add(ColumnDefinition(GridLength(lower_norm, GridUnitType.Star)))
                // Column for the CI bar itself
                defs.Add(ColumnDefinition(GridLength(upper_norm - lower_norm, GridUnitType.Star)))
                // Column for the space after the CI bar
                defs.Add(ColumnDefinition(GridLength(1.0 - upper_norm, GridUnitType.Star)))
                defs

            DockPanel.create [
                DockPanel.children [
                    TextBlock.create [
                        TextBlock.text (sprintf "%s: %d wins (%.2f%%)" name wins winRatePercent)
                        TextBlock.margin 5.0
                    ]

                    // Graphical representation using a Grid
                    Grid.create [
                        Grid.height 20.0
                        Grid.width 300.0
                        Grid.margin 5.0
                        Grid.background "lightgray"
                        Grid.columnDefinitions colDefs
                        Grid.children [
                            // The confidence interval bar
                            Border.create [
                                Border.background "cornflowerblue"
                                Grid.column 1
                            ]

                            // A marker for the actual win rate
                            Border.create [
                                Border.background "black"
                                Border.width 2.0
                                Border.horizontalAlignment HorizontalAlignment.Left
                                Grid.columnSpan 3
                                // Position the marker based on its normalized position
                                Border.margin (p_norm * 300.0 - 1.0, 0.0, 0.0, 0.0)
                            ]
                        ]
                    ]

                    TextBlock.create [
                        TextBlock.text (sprintf "95%% CI: [%.2f%%, %.2f%%]" (lower * 100.0) (upper * 100.0))
                        TextBlock.margin 5.0
                    ]
                ]
            ]


    // The main view of the application
    let view (model: Model) (dispatch: Message -> unit) =
        // Calculate the CIs for both teams to determine the zoom level
        let ewGamesWon, nsGamesWon = model.GamesWon
        let totalGames = ewGamesWon + nsGamesWon
        let ewLower, ewUpper = calculateConfidenceInterval ewGamesWon totalGames
        let nsLower, nsUpper = calculateConfidenceInterval nsGamesWon totalGames

        // Determine the total range covered by both CIs
        let minCI = min ewLower nsLower
        let maxCI = max ewUpper nsUpper
        
        // Add a 10% margin on each side for padding
        let margin = (maxCI - minCI) * 0.1
        
        // Calculate the final zoomed-in view range, clamped between 0 and 1
        let viewMin = max 0.0 (minCI - margin)
        let viewMax = min 1.0 (maxCI + margin)

        DockPanel.create [
            DockPanel.children [
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.text (sprintf "Total Games Played: %d" totalGames)
                    TextBlock.fontSize 24.0
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin 10.0
                ]
                
                // Display the current zoom range
                TextBlock.create [
                    TextBlock.dock Dock.Top
                    TextBlock.text (sprintf "Viewing Range: [%.1f%%, %.1f%%]" (viewMin * 100.0) (viewMax * 100.0))
                    TextBlock.fontSize 14.0
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin (0.0, 0.0, 0.0, 10.0)
                ]

                StackPanel.create [
                    StackPanel.dock Dock.Top
                    StackPanel.horizontalAlignment HorizontalAlignment.Center
                    StackPanel.verticalAlignment VerticalAlignment.Center
                    StackPanel.spacing 20.0
                    StackPanel.children [
                        // Pass the calculated view range to each team's view
                        teamStatsView "E+W" ewGamesWon totalGames viewMin viewMax
                        teamStatsView "N+S" nsGamesWon totalGames viewMin viewMax
                    ]
                ]
            ]
        ]
