namespace Setback.Web.Client

open System
open Browser.Dom
open Fable.Core.JsInterop
open PlayingCards

/// A view of a card.
type CardView = JQueryElement

module CardView =

    /// Creates a card view.
    // https://stackoverflow.com/a/24201249/344223
    let private create src id =
        Promise.create (fun resolve reject ->

                // create the image
            let img = Image.Create(src = src, id = id, alt = id)

                // set image properties view JQuery
            let cardView = ~~img : CardView
            cardView.addClass("card")
            cardView.css
                {|
                    width = "100px"
                    ``z-index`` = JQueryElement.zIndexIncr ()
                |}

                // wait for image to load
            if img.complete then
                resolve cardView
            else
                img.addEventListener("load", fun _ ->
                    resolve cardView)
                img.addEventListener("error", fun _ ->
                    reject (Exception("Could not load image"))))

    /// Card images. (Unfortunately, import only works with string
    /// literals.)
    let private srcMap =
        [
            Card.fromString "2C", importDefault "./assets/card_images/2C.svg"
            Card.fromString "2D", importDefault "./assets/card_images/2D.svg"
            Card.fromString "2H", importDefault "./assets/card_images/2H.svg"
            Card.fromString "2S", importDefault "./assets/card_images/2S.svg"

            Card.fromString "3C", importDefault "./assets/card_images/3C.svg"
            Card.fromString "3D", importDefault "./assets/card_images/3D.svg"
            Card.fromString "3H", importDefault "./assets/card_images/3H.svg"
            Card.fromString "3S", importDefault "./assets/card_images/3S.svg"

            Card.fromString "4C", importDefault "./assets/card_images/4C.svg"
            Card.fromString "4D", importDefault "./assets/card_images/4D.svg"
            Card.fromString "4H", importDefault "./assets/card_images/4H.svg"
            Card.fromString "4S", importDefault "./assets/card_images/4S.svg"

            Card.fromString "5C", importDefault "./assets/card_images/5C.svg"
            Card.fromString "5D", importDefault "./assets/card_images/5D.svg"
            Card.fromString "5H", importDefault "./assets/card_images/5H.svg"
            Card.fromString "5S", importDefault "./assets/card_images/5S.svg"

            Card.fromString "6C", importDefault "./assets/card_images/6C.svg"
            Card.fromString "6D", importDefault "./assets/card_images/6D.svg"
            Card.fromString "6H", importDefault "./assets/card_images/6H.svg"
            Card.fromString "6S", importDefault "./assets/card_images/6S.svg"

            Card.fromString "7C", importDefault "./assets/card_images/7C.svg"
            Card.fromString "7D", importDefault "./assets/card_images/7D.svg"
            Card.fromString "7H", importDefault "./assets/card_images/7H.svg"
            Card.fromString "7S", importDefault "./assets/card_images/7S.svg"

            Card.fromString "8C", importDefault "./assets/card_images/8C.svg"
            Card.fromString "8D", importDefault "./assets/card_images/8D.svg"
            Card.fromString "8H", importDefault "./assets/card_images/8H.svg"
            Card.fromString "8S", importDefault "./assets/card_images/8S.svg"

            Card.fromString "9C", importDefault "./assets/card_images/9C.svg"
            Card.fromString "9D", importDefault "./assets/card_images/9D.svg"
            Card.fromString "9H", importDefault "./assets/card_images/9H.svg"
            Card.fromString "9S", importDefault "./assets/card_images/9S.svg"

            Card.fromString "TC", importDefault "./assets/card_images/TC.svg"
            Card.fromString "TD", importDefault "./assets/card_images/TD.svg"
            Card.fromString "TH", importDefault "./assets/card_images/TH.svg"
            Card.fromString "TS", importDefault "./assets/card_images/TS.svg"

            Card.fromString "JC", importDefault "./assets/card_images/JC.svg"
            Card.fromString "JD", importDefault "./assets/card_images/JD.svg"
            Card.fromString "JH", importDefault "./assets/card_images/JH.svg"
            Card.fromString "JS", importDefault "./assets/card_images/JS.svg"

            Card.fromString "QC", importDefault "./assets/card_images/QC.svg"
            Card.fromString "QD", importDefault "./assets/card_images/QD.svg"
            Card.fromString "QH", importDefault "./assets/card_images/QH.svg"
            Card.fromString "QS", importDefault "./assets/card_images/QS.svg"

            Card.fromString "KC", importDefault "./assets/card_images/KC.svg"
            Card.fromString "KD", importDefault "./assets/card_images/KD.svg"
            Card.fromString "KH", importDefault "./assets/card_images/KH.svg"
            Card.fromString "KS", importDefault "./assets/card_images/KS.svg"

            Card.fromString "AC", importDefault "./assets/card_images/AC.svg"
            Card.fromString "AD", importDefault "./assets/card_images/AD.svg"
            Card.fromString "AH", importDefault "./assets/card_images/AH.svg"
            Card.fromString "AS", importDefault "./assets/card_images/AS.svg"
        ] |> Map

    /// Creates a view of the given card.
    let ofCard card =
        create srcMap.[card] card.String

    /// Image of card back.
    let private srcBack =
        importDefault "./assets/card_images/Back.svg"

    /// Creates a view of a card back.
    let ofBack () =
        create srcBack "Back"

    /// Answers the given card view's underlying card.
    let card (cardView : CardView) =
        cardView.attr("id")
            |> Card.fromString
