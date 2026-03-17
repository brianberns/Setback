namespace Setback

open PlayingCards

/// Players in one or more seats play together as a team.
type Team =
    | EastWest = 0
    | NorthSouth = 1

module Team =

    /// Total number of teams.
    let numTeams =
        Enum.getValues<Team>.Length

    /// Display string.
    let toString = function
        | Team.EastWest -> "E+W"
        | Team.NorthSouth -> "N+S"
        | _ -> failwith "Unexpected team"

    /// East/West team.
    let private ewSeats = set [ Seat.East; Seat.West ]

    /// North/South team.
    let private nsSeats = set [ Seat.North; Seat.South ]

    /// Seats that play in the given team.
    let seats = function
        | Team.EastWest -> ewSeats
        | Team.NorthSouth -> nsSeats
        | _ -> failwith "Unexpected team"

    /// Team on which the given seat plays.
    let ofSeat = function
        | Seat.East | Seat.West -> Team.EastWest
        | Seat.North | Seat.South -> Team.NorthSouth
        | _ -> failwith "Unexpected seat"
