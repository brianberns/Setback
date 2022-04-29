namespace Setback.Web.Client

open System
open Browser.Dom

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    // Get a reference to our button and cast the Element to an HTMLButtonElement
    let myButton = document.querySelector(".my-button") :?> Browser.Types.HTMLButtonElement
    let myList = document.querySelector(".my-list") :?> Browser.Types.HTMLUListElement

    let rng = Random()
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck Seat.South
    console.log(deal)

    // Register our listener
    myButton.onclick <- fun _ ->
        async {
            let! indexOpt =
                Remoting.api.GetActionIndex("...+03t....0TJTQTKWx")
            let item =
                document.createElement("li")
                    |> myList.appendChild
            let text = sprintf "%A" indexOpt
            document.createTextNode(text)
                |> item.appendChild
                |> ignore
        } |> Async.StartImmediate
