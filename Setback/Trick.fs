namespace Setback

open PlayingCards

/// One card played by each player in turn during a deal.
type Trick =
    {
        /// Player who starts this trick.
        Leader : Seat

        /// Cards played by seat in this trick, in reverse chronological order.
        Cards : List<Card>

        /// Play that takes this trick, so far, if any.
        HighPlayOpt : Option<Seat * Card>
    }

    /// Suit of first card played in this trick, if any.
    member trick.SuitLedOpt =
        trick.HighPlayOpt
            |> Option.map (snd >> Card.suit)

module Trick =

    /// Creates a trick with the given leader to play first.
    let create leader =
        {
            Leader = leader
            Cards = List.empty
            HighPlayOpt = None
        }

    /// Current player on the given trick.
    let currentPlayer trick =
        trick.Leader
            |> Seat.incr trick.Cards.Length

    /// The high player on this trick, if any.
    let highPlayerOpt trick =
        trick.HighPlayOpt
            |> Option.map fst

    /// Plays the given card on the given trick.
    let addPlay trump card trick =
        assert(trick.Cards.Length < Seat.numSeats)
        {
            trick with
                Cards = card :: trick.Cards
                HighPlayOpt =
                    let isHigh =
                        trick.HighPlayOpt
                            |> Option.map (fun (_, prevCard) ->
                                if prevCard.Suit = trump then
                                    prevCard.Suit <> trump
                                        || card.Rank > prevCard.Rank
                                elif card.Suit = prevCard.Suit then
                                    card.Rank > prevCard.Rank
                                else false)
                            |> Option.defaultValue true
                    if isHigh then
                        Some (currentPlayer trick, card)
                    else trick.HighPlayOpt
        }

    /// Indicates whether the given trick has finished.
    let isComplete trick =
        assert(trick.Cards.Length <= Seat.numSeats)
        trick.Cards.Length = Seat.numSeats

    /// Each card in the given trick and its player, in chronological order.
    let plays trick =
        let seats = Seat.cycle trick.Leader
        let cards = Seq.rev trick.Cards
        Seq.zip seats cards
