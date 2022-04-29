namespace Server

open Suave
open Suave.Logging
open Suave.Operators

open Fable.Remoting.Server
open Fable.Remoting.Suave

open Setback.Web

module Program =

    let setbackApi =
        let getActionIndex =
            Setback.Cfrm.DatabasePlayer.init "Setback.db"
        {
            GetActionIndex = getActionIndex >> async.Return
        }

    let logger = Targets.create LogLevel.Info [||]
    let webApp =
        (Remoting.createApi()
            |> Remoting.fromValue setbackApi
            |> Remoting.buildWebPart)
            >=> Filters.logWithLevelStructured
                LogLevel.Info
                logger
                Filters.logFormatStructured

    // start the web server
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 5000 ] }
    startWebServer config webApp
