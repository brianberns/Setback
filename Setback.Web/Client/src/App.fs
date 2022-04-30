namespace Setback.Web.Client

open Browser.Dom

open Fable.Core.JsInterop

open PlayingCards
open Setback
open Setback.Cfrm

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
