namespace Setback

open PlayingCards

/// A location occupied by a player.
type Seat =
    | West  = 0
    | North = 1
    | East  = 2
    | South = 3

module Seat =

    /// Total number of seats.
    let numSeats =
        Enum.getValues<Seat>.Length

    /// Converts the given seat to a character.
    let toChar seat =
        "WNES".[int seat]

#if FABLE_COMPILER
    let toString = function
        | Seat.West -> "West"
        | Seat.North -> "North"
        | Seat.East -> "East"
        | Seat.South -> "South"
#endif

    /// Converts the given character to a seat.
    let fromChar = function
        | 'W' -> Seat.West
        | 'N' -> Seat.North
        | 'E' -> Seat.East
        | 'S' -> Seat.South
        | _ -> failwith "Unexpected seat"

    /// Nth seat after the given seat.
    let incr n (seat : Seat) =
        assert(n >= 0)
        (int seat + n) % numSeats
            |> enum<Seat>

    /// Seat that plays after the given seat.
    let next = incr 1

    /// All seats in order starting with the given seat.
    let cycle seat =
        seq {
            for i = 0 to numSeats - 1 do
                yield seat |> incr i
        }

[<AutoOpen>]
module SeatExt =
    type Seat with

        /// Character representation of this seat.
        member seat.Char = seat |> Seat.toChar

        /// Seat that plays after this seat.
        member seat.Next = seat |> Seat.next
