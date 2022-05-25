namespace Server

module private Remoting =

    open System.IO
    open System.Reflection

    open Fable.Remoting.Server
    open Fable.Remoting.Suave

    open Setback.Web

    /// Get bid/play for any Setback situation.
    let private setbackApi =
        let getActionIndex =
            let dbPath =
                let dir =
                    Assembly.GetExecutingAssembly().Location
                        |> Path.GetDirectoryName
                Path.Combine(dir, "Setback.db")
            Setback.Cfrm.DatabasePlayer.init dbPath
        {
            GetActionIndex =
                fun key -> async { return getActionIndex key }
        }

    /// Build API.
    let webPart =
        Remoting.createApi()
            |> Remoting.fromValue setbackApi
            |> Remoting.buildWebPart

module WebPart =

    open Suave
    open Suave.Filters
    open Suave.Operators

    /// Web part.
    let app =
        choose [
            Remoting.webPart
            GET >=> Files.browseHome
        ]

