namespace Setback.Web.Client

open System

open Browser.Dom
open Browser.Types

open PlayingCards
open Setback
open Setback.Cfrm

module App =

    let rng = Random()
    let dealer = Seat.South
    let deal =
        Deck.shuffle rng
            |> AbstractOpenDeal.fromDeck dealer

    let toPixels str =
        let length = Length.parse str
        match length.Unit with
            | Pixel -> length.Magnitude
            | _ -> failwith "Unit not supported"

    JQuery.init ()

    (~~document).ready (fun () ->

        let surface = JQuery.select "#surface"
        let maxWidth = surface.css "width" |> toPixels
        let maxHeight = surface.css "height" |> toPixels

        let cardViews =
            [|
                for i = 0 to 51 do
                    let card = Card.allCards.[i]
                    let cardView = CardView.create i card
                    surface.append(cardView)
                    cardView
            |]

        for i = 51 downto 0 do
            let cardView = cardViews.[i]
            let left = Coord.toLength maxWidth 0.0
            let top = Coord.toLength maxHeight 0.0
            cardView |> CardView.moveTo left top)
