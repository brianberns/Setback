namespace Setback

open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL

type Model =
    | Start
    | Initialized
    | NewGameStarted

module Model =

    let init () = Start

type MessageKey =
    | Initialize = 1
    | StartNewGame = 2

type Message =
    {
        Key : MessageKey
        Values : int[]
    }

module Message =

    let onInitialize (values : _[]) model =
        let response =
            if values[0] = 0
                && values[1] &&& 1 = 1
                && values[1] &&& 2 = 0 then 0
            else -1
        let key = int MessageKey.Initialize + 100
        Killer.writeMessage key response
        Initialized

    let onStartNewGame (values : _[]) model =
        NewGameStarted

    let update (message : Message) model =
        match message.Key with
            | MessageKey.Initialize ->
                onInitialize message.Values model
            | MessageKey.StartNewGame ->
                onStartNewGame message.Values model
            | _ -> failwith $"Unexpected message key: {message.Key}"

 module View =

    let view model dispatch =

        let tokens = Killer.readMessage ()
        dispatch {
            Key = enum<MessageKey> tokens[0]
            Values = tokens[1..]
        }

        DockPanel.create [
        ]
