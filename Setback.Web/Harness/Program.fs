open System.Net

open Suave
open Suave.Filters
open Suave.Logging
open Suave.Operators

let config =
    {
        defaultConfig with
            bindings =
                [ HttpBinding.create HTTP IPAddress.Any 5000us ]
    }

let app =
    let logger = Targets.create LogLevel.Info [||]
    choose [
        Dynamic.WebPart.fromToml "WebParts.toml"
        RequestErrors.NOT_FOUND "Found no handlers."
    ] >=> logWithLevelStructured
        LogLevel.Info
        logger
        logFormatStructured

startWebServer config app
