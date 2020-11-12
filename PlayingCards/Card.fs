namespace PlayingCards

/// A card has a rank and a suit.
[<StructuredFormatDisplay("{String}")>]
type Card =
    struct

        /// Rank of this card.
        val Rank : Rank

        /// Suit of this card.
        val Suit : Suit

        /// Constructs a card.
        new(rank : Rank, suit : Suit) =
            { Rank = rank; Suit = suit }

    end

    /// Converts this card to a string.
    override this.ToString() =
        sprintf "%A of %A" this.Rank this.Suit

    /// String representation of this card.
    member this.String = this.ToString()

module Card =

    /// Number of cards in a deck.
    let numCards = Suit.numSuits * Rank.numRanks

    /// Converts a two-character string into a card.
    let fromString (str : string) =
        let rank = Rank.fromChar str.[0]
        let suit = Suit.fromChar str.[1]
        Card(rank, suit)

    /// All the cards in a deck, in order.
    let allCards =
        [|
            for suit in Enum.getValues<Suit> do
                for rank in Enum.getValues<Rank> do
                    yield Card(rank, suit)
        |]
