namespace PlayingCards

open System

/// A "hand of cards" is somewhat ambiguous. Here we use it to
/// refer to a single player's cards.
type Hand = Set<Card>

module Hand =

    /// Converts the given hand to a string.
    /// E.g. "K63♠ 2♥ 96♣"
    let toString (hand : Hand) =
        hand
            |> Seq.groupBy Card.suit
            |> Seq.sortByDescending fst
            |> Seq.map (fun (suit, cards) ->
                let sCards =
                    cards
                        |> Seq.sortByDescending Card.rank
                        |> Seq.map (Card.rank >> Rank.toChar)
                        |> Seq.toArray
                        |> String
                $"{sCards}{suit.Char}")
            |> String.concat " "
