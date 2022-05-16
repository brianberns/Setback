namespace Server

module Logging =

    open Suave.Filters
    open Suave.Logging

    let logger = Log.create "log"

    let webPart =
        logWithLevelStructured
            LogLevel.Info
            logger
            logFormatStructured

module Remoting =

    open Suave.Logging

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
                        let actionIndex = getActionIndex key
                        Logging.logger.info (
                            Message.eventX $"GetActionIndex(\"{key}\") = {actionIndex}")
                        return actionIndex
                    }
        }

    let webPart =
        Remoting.createApi()
            |> Remoting.fromValue setbackApi
            |> Remoting.buildWebPart

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
