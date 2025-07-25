namespace Setback

/// Players in one or more seats play together as a team.
type Team =
    | EastWest = 0
    | NorthSouth = 1

module Team =

    /// Display string.
    let toString = function
        | Team.EastWest -> "E+W"
        | Team.NorthSouth -> "N+S"
        | _ -> failwith "Unexpected team"

    let private ewTeam = set [ Seat.East; Seat.West ]
    let private nsTeam = set [ Seat.North; Seat.South ]

    /// Seats that play in the given team.
    let seats = function
        | Team.EastWest -> ewTeam
        | Team.NorthSouth -> nsTeam
        | _ -> failwith "Unexpected team"

    /// Team on which the given seat plays.
    let ofSeat = function
        | Seat.East | Seat.West -> Team.EastWest
        | Seat.North | Seat.South -> Team.NorthSouth
        | _ -> failwith "Unexpected seat"
