namespace Setback.Web.Client

open System
open Browser.Dom

open Fable.Core.JsInterop

open PlayingCards
open Setback
open Setback.Cfrm

module Img =

    let private cardMap =
        [
            Card.fromString "2C", importDefault "cardsJS/cards/2C.svg"
            Card.fromString "2D", importDefault "cardsJS/cards/2D.svg"
            Card.fromString "2H", importDefault "cardsJS/cards/2H.svg"
            Card.fromString "2S", importDefault "cardsJS/cards/2S.svg"

            Card.fromString "3C", importDefault "cardsJS/cards/3C.svg"
            Card.fromString "3D", importDefault "cardsJS/cards/3D.svg"
            Card.fromString "3H", importDefault "cardsJS/cards/3H.svg"
            Card.fromString "3S", importDefault "cardsJS/cards/3S.svg"

            Card.fromString "4C", importDefault "cardsJS/cards/4C.svg"
            Card.fromString "4D", importDefault "cardsJS/cards/4D.svg"
            Card.fromString "4H", importDefault "cardsJS/cards/4H.svg"
            Card.fromString "4S", importDefault "cardsJS/cards/4S.svg"

            Card.fromString "5C", importDefault "cardsJS/cards/5C.svg"
            Card.fromString "5D", importDefault "cardsJS/cards/5D.svg"
            Card.fromString "5H", importDefault "cardsJS/cards/5H.svg"
            Card.fromString "5S", importDefault "cardsJS/cards/5S.svg"

            Card.fromString "6C", importDefault "cardsJS/cards/6C.svg"
            Card.fromString "6D", importDefault "cardsJS/cards/6D.svg"
            Card.fromString "6H", importDefault "cardsJS/cards/6H.svg"
            Card.fromString "6S", importDefault "cardsJS/cards/6S.svg"

            Card.fromString "7C", importDefault "cardsJS/cards/7C.svg"
            Card.fromString "7D", importDefault "cardsJS/cards/7D.svg"
            Card.fromString "7H", importDefault "cardsJS/cards/7H.svg"
            Card.fromString "7S", importDefault "cardsJS/cards/7S.svg"

            Card.fromString "8C", importDefault "cardsJS/cards/8C.svg"
            Card.fromString "8D", importDefault "cardsJS/cards/8D.svg"
            Card.fromString "8H", importDefault "cardsJS/cards/8H.svg"
            Card.fromString "8S", importDefault "cardsJS/cards/8S.svg"

            Card.fromString "9C", importDefault "cardsJS/cards/9C.svg"
            Card.fromString "9D", importDefault "cardsJS/cards/9D.svg"
            Card.fromString "9H", importDefault "cardsJS/cards/9H.svg"
            Card.fromString "9S", importDefault "cardsJS/cards/9S.svg"

            Card.fromString "TC", importDefault "cardsJS/cards/10C.svg"
            Card.fromString "TD", importDefault "cardsJS/cards/10D.svg"
            Card.fromString "TH", importDefault "cardsJS/cards/10H.svg"
            Card.fromString "TS", importDefault "cardsJS/cards/10S.svg"

            Card.fromString "JC", importDefault "cardsJS/cards/JC.svg"
            Card.fromString "JD", importDefault "cardsJS/cards/JD.svg"
            Card.fromString "JH", importDefault "cardsJS/cards/JH.svg"
            Card.fromString "JS", importDefault "cardsJS/cards/JS.svg"

            Card.fromString "QC", importDefault "cardsJS/cards/QC.svg"
            Card.fromString "QD", importDefault "cardsJS/cards/QD.svg"
            Card.fromString "QH", importDefault "cardsJS/cards/QH.svg"
            Card.fromString "QS", importDefault "cardsJS/cards/QS.svg"

            Card.fromString "KC", importDefault "cardsJS/cards/KC.svg"
            Card.fromString "KD", importDefault "cardsJS/cards/KD.svg"
            Card.fromString "KH", importDefault "cardsJS/cards/KH.svg"
            Card.fromString "KS", importDefault "cardsJS/cards/KS.svg"

            Card.fromString "AC", importDefault "cardsJS/cards/AC.svg"
            Card.fromString "AD", importDefault "cardsJS/cards/AD.svg"
            Card.fromString "AH", importDefault "cardsJS/cards/AH.svg"
            Card.fromString "AS", importDefault "cardsJS/cards/AS.svg"
        ]
            |> Seq.map (fun (card, src) ->
                let img = Image.Create(src = src)
                img.classList.add("card")
                card, img)
            |> Map

    let ofCard card =
        cardMap.[card]

module HandDiv =

    let private divMap =
        Enum.getValues<Seat>
            |> Seq.map (fun seat ->
                let div =
                    let name = Seat.toString seat
                    let id = $"hand-{name.ToLower()}"
                    document.getElementById(id)
                        :?> Browser.Types.HTMLDivElement
                console.log(div)
                seat, div)
            |> Map

    let ofSeat seat =
        divMap.[seat]

    let addCard card (div : Browser.Types.HTMLDivElement) =
        card
            |> Img.ofCard
            |> div.appendChild
            |> ignore

module App =

    importAll "cardsJS/cards.css"
    importAll "cardsJS/cards.js"

    let pairs =
        [
            Seat.West, Suit.Spades
            Seat.North, Suit.Diamonds
            Seat.East, Suit.Clubs
            Seat.South, Suit.Hearts
        ]
    for (seat, suit) in pairs do
        let div = HandDiv.ofSeat seat
        Card.allCards
            |> Seq.where (fun card -> card.Suit = suit)
            |> Seq.iter (fun card -> HandDiv.addCard card div)


    (*
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
    *)
