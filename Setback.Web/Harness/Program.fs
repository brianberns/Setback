open System.Net

open Suave
open Suave.Filters
open Suave.Logging
open Suave.Operators

#if DEBUG
let testRng () =
    let rng = PlayingCards.Random(0UL)
    let expected =
        [
            0x3C6EF35FUL
            0x47502932UL
            0xD1CCF6E9UL
            0xAAF95334UL
            0x6252E503UL
            0x9F2EC686UL
            0x57FE6C2DUL
            0xA3D95FA8UL
            0x81FDBEE7UL
            0x94F0AF1AUL
            0xCBF633B1UL
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
