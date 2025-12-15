namespace PlayingCards

open System

/// Clonable random number generator.
/// https://en.wikipedia.org/wiki/Linear_congruential_generator
type Random(seed) =

    /// Blessed parameters (Knuth MMIX).
    /// m is effectively 2^64 (implicit via uint64 overflow).
    static let a = 6364136223846793005UL
    static let c = 1442695040888963407UL

    /// Computes the next state.
    static let next cur =
        a * cur + c

    /// Current state.
    let mutable state : uint64 = seed

    /// Constructs an RNG with an arbitrary seed.
    new() =
        let seed =
            DateTime.Now.Ticks
                |> uint64
                |> next
        Random(seed)

    /// Answers a random number in the given range.
    member _.Next(minValue, maxValue) =
        let range = uint64 (maxValue - minValue)
        if range <= 0UL then failwith "Invalid range"
        state <- next state
        
            // use upper 32 bits to avoid LCG low-bit patterns.
        let result = state >>> 32
        (int (result % range)) + minValue

    /// Clones the RNG in its current state.
    member _.Clone() = Random(state)

    /// Current state of the RNG.
    member _.State = state

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
            let item = items[i]
            items[i] <- items[j]
            items[j] <- item
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
