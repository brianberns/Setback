namespace Setback.Web.Client

open Browser

open Setback

module Session =

    /// Runs a new session.
    let run surface persState =

        /// Plays games forever.
        let rec loop persState =
            async {
                let! persState = Game.run surface persState
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
