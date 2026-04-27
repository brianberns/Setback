namespace Setback.Web.Client

open Browser

open Setback

module Session =

    /// Runs a new session.
    let run surface persState =

        /// Plays games forever.
        let rec loop persState =
            async {
                    // run current game
                let! persState = Game.run surface persState

                    // start another game
                let game =
                    persState.Game.Deal.ClosedDeal.Dealer
                        |> Seat.incr 1
                        |> Game.create Random.Shared
                console.log($"Dealer is {Seat.toString game.Deal.ClosedDeal.Auction.Dealer}")   // ugly: starting a new game also starts the first deal in that game
                let persState = { persState with Game = game }.Save()

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
