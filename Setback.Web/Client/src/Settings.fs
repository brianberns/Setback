namespace Setback.Web.Client

open Browser
open Fable.SimpleJson

type Settings =
    {
        /// Animation speed.
        AnimationSpeed : int
    }

module Settings =

    /// Creates initial settings.
    let create () =
        {
            AnimationSpeed = 300
        }

    /// Local storage key.
    let private key = "Settings"

    /// Saves the given settings.
    let save (settings : Settings) =
        WebStorage.localStorage[key]
            <- Json.serialize settings

    /// Answers the current settings.
    let get () =
        let json = WebStorage.localStorage[key]
        if isNull json then
            let settings = create ()
            save settings
            settings
        else
            Json.parseAs<Settings>(json)

type Settings with

    /// Current settings.
    static member Current =
        Settings.get()
