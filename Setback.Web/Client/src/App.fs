namespace Setback.Web.Client

open Browser

open PlayingCards
open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface rng dealer =

        let rec loop (rng : Random) dealer =
            async {
                    // run first game of a pair
                let rngBefore = rng.Clone()
                let! dealer1, nDeals1 = Game.run surface rng dealer
                let rngAfter1 = rng.Clone()

                    // run second game of the pair w/ duplicate deals
                let! dealer2, nDeals2 = Game.run surface rngBefore dealer.Next
                let rngAfter2 = rng.Clone()

                    // continue with unseen RNG state
                assert(nDeals1 <> nDeals2 || rngAfter1.State = rngAfter2.State)
                let rngAfter, dealer' =
                    if nDeals1 > nDeals2 then rngAfter1, dealer1
                    else rngAfter2, dealer2
                do! loop rngAfter dealer'
            }

        async {
            try
                do! loop rng dealer
            with ex ->
                console.log(ex.StackTrace)
                window.alert(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        let rng = Random()
        Session.run surface rng Seat.South)
