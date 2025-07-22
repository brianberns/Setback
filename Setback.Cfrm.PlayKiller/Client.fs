namespace Setback

open System
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

    let private onMasterFileChanged dispatch _args =
        let tokens = Killer.readMessage ()
        dispatch {
            Key = enum<MessageKey> tokens[0]
            Values = tokens[1..]
        }

    let private watch : Subscribe<_> =
        fun dispatch ->

            let watcher =
                new FileSystemWatcher(
                    @"C:\Program Files\KSetback",
                    "KSetback.msg.master",
                    EnableRaisingEvents = true)
            watcher.Changed.Add(
                onMasterFileChanged dispatch)

            {
                new IDisposable with
                    member _.Dispose() =
                        watcher.Dispose()
            }

    let subscribe model : Sub<_> =
        [
            [ "watch" ], watch
        ]

 module View =

    let view model dispatch =
        DockPanel.create [
        ]
