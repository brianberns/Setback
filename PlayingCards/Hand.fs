namespace PlayingCards

open System

/// A "hand of cards" is somewhat ambiguous. Here we use it to refer to
/// a single player's cards.
type Hand = seq<Card>

module Hand =

    /// Converts the given hand to a string.
    /// E.g. "K63♠ 2♥ 96♣"
    let toString (hand : Hand) =
        hand
            |> Seq.groupBy (fun card -> card.Suit)
            |> Seq.sortByDescending fst
            |> Seq.map (fun (suit, cards) ->
                let sCards =
                    cards
                        |> Seq.sortByDescending (fun card -> card.Rank)
                        |> Seq.map (fun card -> card.Rank.Char)
                        |> Seq.toArray
                        |> String
                $"{sCards}{suit.Char}")
            |> String.concat " "
