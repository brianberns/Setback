namespace PlayingCards

/// A card has a rank and a suit.
[<StructuredFormatDisplay("{String}")>]
type Card =
#if FABLE_COMPILER
    {
        /// Rank of this card.
        Rank : Rank

        /// Suit of this card.
        Suit : Suit
    }
#else
    struct

        /// Rank of this card.
        val Rank : Rank

        /// Suit of this card.
        val Suit : Suit

        /// Constructs a card.
        new(rank : Rank, suit : Suit) =
            { Rank = rank; Suit = suit }

    end
#endif

    /// Converts this card to a string.
    override this.ToString() =
 #if FABLE_COMPILER
        sprintf "%c%c"
            (Rank.toChar this.Rank)
            (Suit.toChar this.Suit)
#else
        System.String.Create(
            2,
            this,
            System.Buffers.SpanAction(fun span (card : Card) ->
                span.Slice(0, 1).Fill(card.Rank.Char)
                span.Slice(1, 1).Fill(card.Suit.Char)))
#endif

    /// String representation of this card.
    member this.String = this.ToString()

module Card =

    /// Creates a card.
    let inline create rank suit =
#if FABLE_COMPILER
        { Rank = rank; Suit = suit }
#else
        Card(rank, suit)
#endif

    /// Number of cards in a deck.
    let numCards = Suit.numSuits * Rank.numRanks

    /// Rank of the given card.
    let rank (card : Card) = card.Rank

    /// Suit of the given card.
    let suit (card : Card) = card.Suit

    /// Converts a two-character string into a card.
    let fromString (str : string) =
        assert(str.Length = 2)
        let rank = Rank.fromChar str[0]
        let suit = Suit.fromChar str[1]
        create rank suit

    /// All the cards in a deck, in order.
    let allCards =
        [|
            for suit in Enum.getValues<Suit> do
                for rank in Enum.getValues<Rank> do
                    yield create rank suit
        |]

    /// Rank of lowest card in the deck.
    let private minRank =
        Seq.min Enum.getValues<Rank>

    /// Converts the given card to an integer, 0..N-1,
    /// where N is number of cards in the deck.
    let toIndex (card : Card) =
        let index =
            (int card.Suit * Rank.numRanks)
                + int card.Rank - int minRank
        assert(index >= 0)
        assert(index < numCards)
        index
