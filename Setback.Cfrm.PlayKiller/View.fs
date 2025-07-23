namespace Setback

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout

module View =
 
    /// 95% confidence interval.
    let getConfidenceInterval numWins numGamesPlayed =
        assert(numGamesPlayed > 0)
        assert(numWins <= numGamesPlayed)
        let p = float numWins / float numGamesPlayed
        let n = float numGamesPlayed
        let z = 1.96   // Z-score for 95% confidence
        let marginOfError = z * sqrt (p * (1.0 - p) / n)
        max 0.0 (p - marginOfError),
        min 1.0 (p + marginOfError)

    let createConfidenceIntervalBar numGamesWon numGamesPlayed viewMin viewMax  =
        assert(numGamesPlayed > 0)
        assert(viewMax > viewMin)

        let winRate = float numGamesWon / float numGamesPlayed
        let lower, upper = getConfidenceInterval numGamesWon numGamesPlayed
        assert(lower >= viewMin)
        assert(upper <= viewMax)

            // normalize to the dynamic view range [viewMin, viewMax]
        let viewSpan = viewMax - viewMin
        let lowerNorm = (lower - viewMin) / viewSpan
        let upperNorm = (upper - viewMin) / viewSpan
        let winRateNorm = (winRate - viewMin) / viewSpan

        let barWidth = 300.0
        Grid.create [
            Grid.height 40.0
            Grid.width barWidth
            Grid.margin 5.0
            Grid.rowDefinitions "1*, 1*"
            Grid.columnDefinitions $"{lowerNorm}*, {upperNorm - lowerNorm}*, {1.0 - upperNorm}*"
            Grid.children [

                    // background
                Border.create [
                    Border.background "lightgray"
                    Border.columnSpan 3
                ]

                    // confidence interval
                Border.create [
                    Border.background "cornflowerblue"
                    Grid.column 1
                ]

                    // marker for the actual win rate
                let markerWidth = 2.0
                Border.create [
                    Border.background "black"
                    Border.width markerWidth
                    Border.horizontalAlignment HorizontalAlignment.Left
                    Grid.columnSpan 3
                    Border.margin (
                        winRateNorm * barWidth - (markerWidth / 2.0),
                        0.0, 0.0, 0.0)
                ]

                TextBlock.create [
                    TextBlock.text $"%.1f{winRate * 100.0}%%"
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.row 1
                    TextBlock.column 1
                ]
            ]
        ]

    // The main view of the application
    let view (model: Model) (dispatch: Message -> unit) =

        let ewGamesWon, nsGamesWon = model.GamesWon
        let numGamesPlayed = ewGamesWon + nsGamesWon

        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.verticalAlignment VerticalAlignment.Center
            StackPanel.spacing 20.0
            StackPanel.children [
                if numGamesPlayed >= 2 then

                    // Calculate the CIs for both teams to determine the zoom level
                    let ewLower, ewUpper = getConfidenceInterval ewGamesWon numGamesPlayed
                    let nsLower, nsUpper = getConfidenceInterval nsGamesWon numGamesPlayed

                    // Determine the total range covered by both CIs
                    let minCI = min ewLower nsLower
                    let maxCI = max ewUpper nsUpper
        
                    // Add a 10% margin on each side for padding
                    let margin = (maxCI - minCI) * 0.1
        
                    // Calculate the final zoomed-in view range, clamped between 0 and 1
                    let viewMin = max 0.0 (minCI - margin)
                    let viewMax = min 1.0 (maxCI + margin)

                    createConfidenceIntervalBar ewGamesWon numGamesPlayed viewMin viewMax
                    createConfidenceIntervalBar nsGamesWon numGamesPlayed viewMin viewMax
            ]
        ]
