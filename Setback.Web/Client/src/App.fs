namespace Setback.Web.Client

open Browser

open Setback
open Setback.Cfrm

module Session =

    /// Runs a new session.
    let run surface persState =

        /// Plays a pair of duplicate games.
        let rec loop persState =
            async {
                if persState.DealOpt.IsSome then

                        // run game in progress
                    console.log("Finishing game in progress")
                    let! persState, _ = Game.run surface persState
                    do! loop persState

                else
                        // run first game of a pair
                    console.log("Duplicate game: 1 of 2")
                    let! persState1, nDeals1 = Game.run surface persState

                        // run second game of a pair w/ duplicate deals
                    console.log("Duplicate game: 2 of 2")
                    let persState =
                        { persState1 with
                            RandomState = persState.RandomState   // reset RNG to repeat deals
                            Dealer = persState.Dealer.Next }      // rotate from first dealer of previous game
                            .Save()
                    let! persState2, nDeals2 = Game.run surface persState
                    assert(nDeals1 <> nDeals2
                        || persState1.RandomState = persState2.RandomState)

                        // continue with unseen random state
                    let persState =
                        let rndState =
                            if nDeals1 > nDeals2 then persState1.RandomState
                            else persState2.RandomState
                        { persState2 with
                            RandomState = rndState
                            Dealer = persState.Dealer.Next }
                            .Save()
                    do! loop persState
        }

        async {
            try
                do! loop persState
            with ex ->
                console.log(ex.StackTrace)
                window.alert(ex.StackTrace)
        } |> Async.StartImmediate

module App =

        // track animation speed slider
    let speedSlider = ~~"#animationSpeed"
    speedSlider.``val``(Settings.Current.AnimationSpeed)
    speedSlider.change(fun () ->
        { AnimationSpeed = speedSlider.``val``() }
            |> Settings.save)

        // start a session when the browser is ready
    (~~document).ready(fun () ->
        let surface = ~~"main"
        PersistentState.get () |> Session.run surface)
