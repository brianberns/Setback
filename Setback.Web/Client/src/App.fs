namespace Setback.Web.Client

open Browser

open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface sessionState =

        /// Plays a pair of duplicate deals.
        let rec loop sessionState =
            async {
                match sessionState.DuplicateDealState with

                        // first game of a pair has already run
                    | Some (randomState1, nDeals1) ->
                        let sessionState1 =
                            { sessionState with RandomState = randomState1 }
                        return! finish sessionState1 nDeals1

                        // run first game of a pair
                    | None ->
                        let! sessionState1, nDeals1 = Game.run surface sessionState
                        return! finish sessionState1 nDeals1
            }

        and finish sessionState1 nDeals1 =
            async {
                    // run second game of the pair w/ duplicate deals
                let sessionState' =
                    { sessionState1 with
                        RandomState = sessionState.RandomState   // reset RNG to duplicate deals
                        DuplicateDealState =
                            Some (sessionState1.RandomState, nDeals1)
                        Dealer = sessionState.Dealer.Next }      // rotate from first dealer of game
                let! sessionState2, nDeals2 = Game.run surface sessionState'
                assert(nDeals1 <> nDeals2
                    || sessionState1.RandomState = sessionState2.RandomState)

                    // continue with unseen random state
                let sessionState' =
                    let randomState =
                        if nDeals1 > nDeals2 then sessionState1.RandomState
                        else sessionState2.RandomState
                    { sessionState2 with
                        RandomState = randomState
                        DuplicateDealState = None
                        Dealer = sessionState.Dealer.Next.Next }
                do! loop sessionState'
        }

        async {
            try
                do! loop sessionState
            with ex ->
                console.log(ex.StackTrace)
                window.alert(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        SessionState.get () |> Session.run surface)
