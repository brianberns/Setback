open System.Net
open Suave

let config =
    {
        defaultConfig with
            bindings =
                [ HttpBinding.create HTTP IPAddress.Any 5000us ]
    }

let app =
    choose [
        Dynamic.WebPart.fromToml "WebParts.toml"
        RequestErrors.NOT_FOUND "Found no handlers."
    ]

startWebServer config app
