namespace Setback

open System
open PlayingCards

module Tournament =

    /// Challenger's team.
    let private challengerTeam = Team.EastWest

    /// Seats occupied by challenger players.
    let private challengerSeats =
        Team.seats challengerTeam

    /// Runs a 2v2 tournament between two players.
    let run inParallel numGames champion challenger =
        let playerMap =
            Enum.getValues<Seat>
                |> Seq.map (fun seat ->
                    let player =
                        if challengerSeats.Contains(seat) then
                            challenger
                        else champion
                    seat, player)
                |> Map
        Game.playGames Random.Shared inParallel numGames (
            Game.playGame Random.Shared playerMap)   // thread-safety needed
            |> Seq.where ((=) challengerTeam)
            |> Seq.length
