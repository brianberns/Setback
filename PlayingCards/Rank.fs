namespace PlayingCards

/// The rank of a card.
type Rank =
    | Two   =  2
    | Three =  3
    | Four  =  4
    | Five  =  5
    | Six   =  6
    | Seven =  7
    | Eight =  8
    | Nine  =  9
    | Ten   = 10
    | Jack  = 11
    | Queen = 12
    | King  = 13
    | Ace   = 14   // because Ace > King in most games

module Rank =

    /// Number of ranks in each suit.
    let numRanks =
        Enum.getValues<Rank>.Length

    /// Converts the given rank to a character.
    let toChar rank = 
        "23456789TJQKA".[int rank - 2]

    /// Converts the given character to a rank.
    let fromChar = function
        | 'T' -> Rank.Ten
        | 'J' -> Rank.Jack
        | 'Q' -> Rank.Queen
        | 'K' -> Rank.King
        | 'A' -> Rank.Ace
        | c ->
            let n = int c - int '0'
            if n >= 2 && n <= 9 then
                enum<Rank> n
            else
                failwith "Unexpected rank char"

[<AutoOpen>]
module RankExt =

    type Rank with

        /// Character representation of this rank.
        member rank.Char = rank |> Rank.toChar
