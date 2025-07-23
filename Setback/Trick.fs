namespace Setback

open PlayingCards

/// One card from each player in turn during a deal.
type Trick =
    {
            // trump suit in this trick
        Trump : Suit

            // suit of first card played in this trick
        SuitLed : Suit

            // cards played by seat in this trick, in reverse chronological order
        Plays : List<Seat * Card>

            // number of cards played so far in this trick
        NumPlays : int

            // winning play, so far
        Winner : Seat * Card
    }

module Trick =

    /// Starts a new trick with the given play.
    let create trump seat (card : Card) =
        {
            Trump = trump
            SuitLed = card.Suit
            Plays = (seat, card) :: []
            NumPlays = 1
            Winner = seat, card
        }

    /// Adds the given play to the given trick.
    let add seat card trick =
        {
            trick with
                Plays = (seat, card) :: trick.Plays
                NumPlays = trick.NumPlays + 1
                Winner =
                    let _, prevCard = trick.Winner
                    let isWinner =
                        if card.Suit = trick.Trump then
                            prevCard.Suit <> trick.Trump
                                || card.Rank > prevCard.Rank
                        elif card.Suit = prevCard.Suit then
                            card.Rank > prevCard.Rank
                        else
                            false
                    if isWinner then (seat, card)
                    else trick.Winner
        }

[<AutoOpen>]
module TrickExt =
    type Trick with
        member trick.Add(seat, card) =
            trick |> Trick.add seat card
        member trick.WinnerSeat =
            trick.Winner |> fst
