namespace Setback

/// Possible points(*) available in a Setback deal.
///
/// *The terminology here gets quite confusing. We use the term "match
/// point" to distinguish these points from "game points". Whichever team
/// gets the most game points in a deal wins the "Game" match point for
/// that deal.
type MatchPoint =
    | High = 0   // highest dealt trump
    | Low  = 1   // lowest dealt trump
    | Jack = 2   // jack of trump (if dealt)
    | Game = 3   // AKQJT of all suits
