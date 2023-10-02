namespace Setback.Web.Client

open Browser

open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface persistentState =

        /// Plays a pair of duplicate deals.
        let rec loop persistentState =
            async {
                match persistentState.DuplicateDealState with

                        // first game of a pair has already run
                    | Some (randomState1, nDeals1) ->
                        let persistentState1 =
                            { persistentState with RandomState = randomState1 }   // reset RNG to repeat deals
                        return! finish persistentState1 nDeals1

                        // run first game of a pair
                    | None ->
                        let! persistentState1, nDeals1 = Game.run surface persistentState
                        return! finish persistentState1 nDeals1
            }

        and finish persistentState1 nDeals1 =
            async {
                    // run second game of the pair w/ duplicate deals
                let persistentState' =
                    { persistentState1 with
                        RandomState = persistentState.RandomState       // reset RNG to repeat deals
                        DuplicateDealState =
                            Some (persistentState1.RandomState, nDeals1)
                        Dealer = persistentState.Dealer.Next }.Save()   // rotate from first dealer of game
                let! persistentState2, nDeals2 = Game.run surface persistentState'
                assert(nDeals1 <> nDeals2
                    || persistentState1.RandomState = persistentState2.RandomState)

                    // continue with unseen random state
                let persistentState' =
                    let randomState =
                        if nDeals1 > nDeals2 then persistentState1.RandomState
                        else persistentState2.RandomState
                    { persistentState2 with
                        RandomState = randomState
                        DuplicateDealState = None
                        Dealer = persistentState.Dealer.Next.Next }.Save()
                do! loop persistentState'
        }

        async {
            try
                do! loop persistentState
            with ex ->
                console.log(ex.StackTrace)
                window.alert(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        PersistentState.get () |> Session.run surface)
