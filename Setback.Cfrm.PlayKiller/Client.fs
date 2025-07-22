namespace Setback

open System
open System.IO

open Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout

type Model =
    | Start
    | Initialized
    | NewGameStarted
    | NewHandDealt of {|
        Dealer : Seat
        EwScore : int
        NsScore : int |}
    | Error of string

module Model =

    let init () = Start

type MessageKey =
    | Initialize = 1
    | StartNewGame = 2
    | DealNewHand = 3

type Message =
    {
        Key : MessageKey
        Values : int[]
    }

module Message =

    let private respond key value =
        let key = int key + 100
        Killer.writeMessage key value

    let private onInitialize message model =
        let response =
            if message.Values[0] = 0
                && message.Values[1] &&& 1 = 1          // play to 11
                && message.Values[1] &&& 2 = 0 then 0   // no smudge
            else -1
        respond message.Key response
        Initialized

    let private onStartNewGame message model =
        respond message.Key 0
        NewGameStarted

    let private onDealNewHand message model =
        respond message.Key 0
        NewHandDealt {|
            Dealer = enum<Seat> message.Values[1]
            EwScore = message.Values[2]
            NsScore = message.Values[3]
        |}

    let update (message : Message) model =
        match message.Key with
            | MessageKey.Initialize ->
                onInitialize message model
            | MessageKey.StartNewGame ->
                onStartNewGame message model
            | MessageKey.DealNewHand ->
                onDealNewHand message model
            | _ -> Error $"Unexpected message key: {message.Key}"

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
        TextBlock.create [
            TextBlock.text (string model)
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.verticalAlignment VerticalAlignment.Center
        ]
