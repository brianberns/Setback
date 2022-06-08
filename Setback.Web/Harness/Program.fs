open System.Net

open Suave
open Suave.Filters
open Suave.Logging
open Suave.Operators

#if DEBUG
let testRng () =
    let rng = PlayingCards.Random(0u)
    let expected =
        [
            0x3C6EF35Fu
            0x47502932u
            0xD1CCF6E9u
            0xAAF95334u
            0x6252E503u
            0x9F2EC686u
            0x57FE6C2Du
            0xA3D95FA8u
            0x81FDBEE7u
            0x94F0AF1Au
            0xCBF633B1u
        ]
    let actual =
        List.init 11 (fun _ ->
            rng.Next(0, 1) |> ignore
            rng.State)
    (expected, actual)
        ||> List.iter2 (fun exp act ->
            assert(exp = act))
#endif

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

#if DEBUG
testRng ()
#endif

startWebServer config app
