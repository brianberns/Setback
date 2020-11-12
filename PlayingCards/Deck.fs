namespace PlayingCards

open System

/// A shuffled deck of cards.
type Deck =
    {
        Cards : Card[]
    }

module Deck =

    /// Shuffles the given array in place.
    /// From http://rosettacode.org/wiki/Knuth_shuffle#F.23
    let private knuthShuffle (rng : Random) (items : _[]) =
        let swap i j =
            let item = items.[i]
            items.[i] <- items.[j]
            items.[j] <- item
        let len = items.Length
        [0 .. len - 2]
            |> Seq.iter (fun i -> swap i (rng.Next(i, len)))
        items

    /// Creates a shuffled deck of cards.
    let shuffle rng =
        {
            Cards =
                Card.allCards
                    |> Array.clone
                    |> knuthShuffle rng
        }
