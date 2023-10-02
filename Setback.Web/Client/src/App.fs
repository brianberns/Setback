namespace Setback.Web.Client

open Browser

open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface persState =

        /// Plays a pair of duplicate deals.
        let rec loop persState =
            async {
                match persState.DuplicateDealState with

                        // first game of a pair has already run
                    | Some (rndState1, nDeals1) ->
                        let persState1 =
                            { persState with RandomState = rndState1 }   // reset RNG to repeat deals
                        return! finish persState1 nDeals1

                        // run first game of a pair
                    | None ->
                        let! persState1, nDeals1 = Game.run surface persState
                        return! finish persState1 nDeals1
            }

        and finish persState1 nDeals1 =
            async {
                    // run second game of the pair w/ duplicate deals
                let persState' =
                    { persState1 with
                        RandomState = persState.RandomState   // reset RNG to repeat deals
                        DuplicateDealState =
                            Some (persState1.RandomState, nDeals1)
                        Dealer = persState.Dealer.Next }      // rotate from first dealer of game
                        .Save()
                let! persState2, nDeals2 = Game.run surface persState'
                assert(nDeals1 <> nDeals2
                    || persState1.RandomState = persState2.RandomState)

                    // continue with unseen rnd state
                let persState' =
                    let rndState =
                        if nDeals1 > nDeals2 then persState1.RandomState
                        else persState2.RandomState
                    { persState2 with
                        RandomState = rndState
                        DuplicateDealState = None
                        Dealer = persState.Dealer.Next.Next }
                        .Save()
                do! loop persState'
        }

        async {
            try
                do! loop persState
            with ex ->
                console.log(ex.StackTrace)
                window.alert(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        PersistentState.get () |> Session.run surface)
