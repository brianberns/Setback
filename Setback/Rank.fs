namespace Setback

open PlayingCards

module Rank =

    /// Answers the value of the given rank towards the Game point.
    let gamePoints = function
        | Rank.Ten   -> 10
        | Rank.Ace   ->  4
        | Rank.King  ->  3
        | Rank.Queen ->  2
        | Rank.Jack  ->  1
        | _          ->  0

[<AutoOpen>]
module RankExt =
    type Rank with

        /// Answers the value of this rank towards the Game point.
        member rank.GamePoints = rank |> Rank.gamePoints
