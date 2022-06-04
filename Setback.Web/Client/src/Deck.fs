namespace PlayingCards

/// https://en.wikipedia.org/wiki/Linear_congruential_generator
type Random(seed) =

    let mutable state = seed

    let m = 1L <<< 32
    let a = 1664525L
    let c = 1013904223L

    new() = Random(System.DateTime.Now.Ticks)

    /// Answers a random number in the given range.
    member _.Next(minValue, maxValue) =
        let range = maxValue - minValue
        assert(range >= 0)
        state <- (a * state + c) % m
        int ((state % int64 range) + int64 minValue)

    /// Clones the RNG in its current state.
    member _.Clone() = Random(state)

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
