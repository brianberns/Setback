namespace Server

module Remoting =

    open Fable.Remoting.Server
    open Fable.Remoting.Suave

    open Setback.Web

    let private setbackApi =
        let getActionIndex =
            Setback.Cfrm.DatabasePlayer.init "Setback.db"
        {
            GetActionIndex =
                fun key ->
                    async {
                        return getActionIndex key
                    }
        }

    let webPart =
        Remoting.createApi()
            |> Remoting.fromValue setbackApi
            |> Remoting.buildWebPart

module Logging =

    open Suave.Filters
    open Suave.Logging

    let webPart =
        let logger = Targets.create LogLevel.Info [||]
        logWithLevelStructured
            LogLevel.Info
            logger
            logFormatStructured

module Program =

    open System.IO
    open System.Net

    open Suave
    open Suave.Filters
    open Suave.Operators

    let config =
        {
            defaultConfig with
                bindings =
                    [ HttpBinding.create HTTP IPAddress.Any 5000us ]
                homeFolder = Some (Path.GetFullPath "./public")
        }

    let webApp =
        choose [
            Remoting.webPart
            GET >=> Files.browseHome
        ] >=> Logging.webPart

    startWebServer config webApp
