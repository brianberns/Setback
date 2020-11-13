namespace Setback

type Team =
    {
        /// Unique identifier of this team (0-based).
        Id : int
    }

module Team =

    /// Total number of teams:
    /// * Team 0: East & West
    /// * Team 1: North & South
    let numTeams = 2

    /// Nth team after the given team.
    let incr n (team : Team) =
        assert(n >= 0)
        { Id = (team.Id + n) % numTeams }

    /// Team that plays after the given team.
    let next = incr 1

    /// All teams in order starting with the given team.
    let cycle team =
        seq {
            for i = 0 to numTeams - 1 do
                yield team |> incr i
        }

[<AutoOpen>]
module TeamExt =

    type Seat with

        /// This seat's team.
        member seat.Team =
            { Id = int seat % Team.numTeams }
