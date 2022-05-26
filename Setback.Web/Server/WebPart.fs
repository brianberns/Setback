namespace Server

module private Remoting =

    open System.IO

    open Fable.Remoting.Server
    open Fable.Remoting.Suave

    open Setback.Web

    /// Get bid/play for any Setback situation.
    let private setbackApi dir =
        let getActionIndex =
            let dbPath = Path.Combine(dir, "Setback.db")
            Setback.Cfrm.DatabasePlayer.init dbPath
        {
            GetActionIndex =
                fun key -> async { return getActionIndex key }
        }

    /// Build API.
    let webPart dir =
        Remoting.createApi()
            |> Remoting.fromValue (setbackApi dir)
            |> Remoting.buildWebPart

module WebPart =

    open System.IO
    open System.Reflection

    open Suave
    open Suave.Filters
    open Suave.Operators

    /// Web part.
    let app =

        let dir =
            Assembly.GetExecutingAssembly().Location
                |> Path.GetDirectoryName
        let staticPath = Path.Combine(dir, "public")

        choose [
            Remoting.webPart dir
            GET >=> Files.browse staticPath
        ]
