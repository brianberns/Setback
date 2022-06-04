namespace Setback.Web.Client

open Browser

open PlayingCards
open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface (rng : Random) dealer =

        let rec loop dealer =
            async {
                let rng' = rng.Clone()
                let! _ = Game.run surface rng dealer
                let! dealer' = Game.run surface rng' (dealer.Next)   // duplicate deals
                do! loop dealer'
            }

        async {
            try
                do! loop dealer
            with ex -> console.log(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        let rng = Random()
        Session.run surface rng Seat.South)
