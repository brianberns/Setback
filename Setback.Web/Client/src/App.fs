namespace Setback.Web.Client

open System

open Browser.Dom
open Browser.Types

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
                        :?> HTMLDivElement
                seat, div)
            |> Map

    let ofSeat seat =
        divMap.[seat]

    let addCard card (div : HTMLDivElement) =
        card
            |> Img.ofCard
            |> div.appendChild
            |> ignore

module App =

    importAll "cardsJS/cards.css"
    importAll "cardsJS/cards.js"

    let rng = Random()
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer
    for iPlayer = 0 to Seat.numSeats - 1 do
        let seat = Seat.incr iPlayer dealer
        let hand =
            deal.UnplayedCards.[iPlayer]
                |> Seq.sortDescending
        let div = HandDiv.ofSeat seat
        for card in hand do
            HandDiv.addCard card div
