module App

open Browser.Dom
open Fable.Remoting.Client
open Setback.Web.Shared

let setbackApi =
    Remoting.createApi()
        |> Remoting.buildProxy<ISetbackApi>

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
let myList = document.querySelector(".my-list") :?> Browser.Types.HTMLUListElement

// Register our listener
myButton.onclick <- fun _ ->
    async {
        let! indexOpt = setbackApi.GetActionIndex "...+03t....0TJTQTKWx"
        let item =
            document.createElement("li")
                |> myList.appendChild
        let text = sprintf "%A" indexOpt
        document.createTextNode(text)
            |> item.appendChild
            |> ignore
    } |> Async.StartImmediate
