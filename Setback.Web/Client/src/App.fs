namespace Setback.Web.Client

open System

open Browser

open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface rng dealer =

        let rec loop dealer =
            async {
                let! dealer' = Game.run surface rng dealer
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
        (~~"#version").text("1.1")
        let surface = ~~"main"
        let rng = Random()
        Session.run surface rng Seat.South)
