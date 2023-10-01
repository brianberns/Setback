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
                if sessionState.GameParity then

                        // run second game of pair
                    let! _, sessionState2 = Game.run surface sessionState

                        // continue with unseen random state
                    let sessionState' =
                        { sessionState2 with
                            GameParity = false
                            Dealer = sessionState.Dealer.Next }
                    do! loop sessionState'
                else
                        // run first game of a pair
                    let! nDeals1, sessionState1 = Game.run surface sessionState

                        // run second game of the pair w/ duplicate deals
                    let sessionState' =
                        { sessionState1 with
                            RandomState = sessionState.RandomState
                            GameParity = true
                            Dealer = sessionState.Dealer.Next }
                    let! nDeals2, sessionState2 = Game.run surface sessionState'
                    assert(nDeals1 <> nDeals2
                        || sessionState1.RandomState = sessionState2.RandomState)

                        // continue with unseen random state
                    let sessionState' =
                        let randomState =
                            if nDeals1 > nDeals2 then sessionState1.RandomState
                            else sessionState2.RandomState
                        { sessionState2 with
                            RandomState = randomState
                            GameParity = false
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
