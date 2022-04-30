namespace Setback.Web.Client

open System
open Browser.Dom

open Fable.Core.JsInterop

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    importAll "cardsJS/cards.css"
    importAll "cardsJS/cards.js"

    let card2C : string = importDefault "cardsJS/cards/2C.svg"
    let card3C : string = importDefault "cardsJS/cards/3C.svg"
    let card4C : string = importDefault "cardsJS/cards/4C.svg"
    let myHand = document.getElementsByClassName("hand").[0] :?> Browser.Types.HTMLDivElement
    let img2 = Image.Create()
    img2.classList.add("card")
    img2.src <- card2C
    myHand.appendChild(img2) |> ignore
    let img3 = Image.Create()
    img3.classList.add("card")
    img3.src <- card3C
    myHand.appendChild(img3) |> ignore
    let img4 = Image.Create()
    img4.classList.add("card")
    img4.src <- card4C
    myHand.appendChild(img4) |> ignore

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
