namespace Setback.Web

open System.IO
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

module WebPart =

    /// Web part.
    let app =

        let dir =
            Assembly.GetExecutingAssembly().Location
                |> Path.GetDirectoryName
        let staticPath = Path.Combine(dir, "public")

        choose [
            Remoting.webPart dir
            Filters.path "/" >=> Files.browseFile staticPath "index.html"
            GET >=> Files.browse staticPath
        ]
