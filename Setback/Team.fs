namespace Setback

open System.Collections.Immutable
open Seat

/// Players in one or more seats play together as a team.
[<StructuredFormatDisplay("{AsString}")>]
type Team =
    {
        Number : int
        Seats : List<Seat>
    }

    override this.ToString() =
        this.Seats
            |> Seq.map _.Char.ToString()
            |> String.concat("+")

module Team =

    /// Is the given seat a member of the given team?
    let contains seat team =
        team.Seats |> List.exists ((=) seat)

    /// An array that assigns zero to each given team.
    let zeroArray (teams : Team[]) =
        ImmutableArray.ZeroCreate(teams.Length)

[<AutoOpen>]
module TeamExt =
    type Team with
        member team.Contains(seat) =
            team |> Team.contains seat
