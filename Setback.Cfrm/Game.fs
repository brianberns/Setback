namespace Setback.Cfrm

open Setback

/// A game of Setback is a sequence of deals that ends when the
/// leading team's score crosses a fixed threshold.
///
/// The terminology is confusing: whichever team accumulates the
/// most deal points (High, Low, Jack, Game) wins the game. Game
/// points (for face cards and tens) don't contribute directly to
/// winning a game.
type Game =
    {
        /// Deal points taken by each team, relative to the current
        /// dealer's team.
        Score : AbstractScore
    }
    
module Game =

    /// A new game with no score.
    let zero = { Score = AbstractScore.zero }

    /// Shifts from dealer-relative to absolute score.
    let absoluteScore (dealer : Seat) score =
        let iDealerTeam = int dealer % Setback.numTeams
        let iAbsoluteTeam =
            (Setback.numTeams - iDealerTeam) % Setback.numTeams
        score |> AbstractScore.shift iAbsoluteTeam

    /// Shifts from absolute to dealer-relative score
    let relativeScore (dealer : Seat) score =
        let iDealerTeam = int dealer % Setback.numTeams
        score |> AbstractScore.shift iDealerTeam

    /// Absolute index of the winning team in the given score, if
    /// any.
    let winningTeamIdxOpt dealer score =
        score
            |> absoluteScore dealer
            |> BootstrapGameState.winningTeamOpt
