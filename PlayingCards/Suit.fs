namespace PlayingCards

/// The suit of a card.
type Suit =
    | Clubs    = 0
    | Diamonds = 1
    | Hearts   = 2
    | Spades   = 3

module Suit =

    /// Number of suits in a deck.
    let numSuits =
        Enum.getValues<Suit>.Length

    /// Converts the given suit to a character.
    let toChar suit =
        "♣♦♥♠".[int suit]

    /// Converts the given suit to a letter character.
    let toLetter suit =
        "CDHS".[int suit]

    /// Converts the given character to a rank.
    let fromChar = function
        | 'C' | '♣' -> Suit.Clubs
        | 'D' | '♦' -> Suit.Diamonds
        | 'H' | '♥' -> Suit.Hearts
        | 'S' | '♠' -> Suit.Spades
        | _ -> failwith "Unexpected suit char"

[<AutoOpen>]
module SuitExt =

    type Suit with

        /// Character representation of this suit.
        member suit.Char = suit |> Suit.toChar

        /// Letter representation of this suit.
        member suit.Letter = suit |> Suit.toLetter
