namespace Setback

open PlayingCards
open Setback.Cfrm

type Model =

    | Start

    | Initialized

    | NewGameStarted of {|
        EwGamesWon : int
        NsGamesWon : int |}

    | Dealing of {|
        EwGamesWon : int
        NsGamesWon : int
        Dealer : Seat
        GameScore : AbstractScore
        NumCards : int
        CardMap : Map<Seat, Card[]> |}

    | Playing of {|
        EwGamesWon : int
        NsGamesWon : int
        Dealer : Seat
        GameScore : AbstractScore
        Deal : AbstractOpenDeal |}

    | HandComplete of {|
        EwGamesWon : int
        NsGamesWon : int
        EwGameScore : int
        NsGameScore : int |}

    | GameComplete of {|
        EwGamesWon : int
        NsGamesWon : int |}

    | Error of string

    member this.GamesWon =
        match this with
            | Start
            | Initialized -> 0, 0
            | NewGameStarted ngs -> ngs.EwGamesWon, ngs.NsGamesWon
            | Dealing dealing -> dealing.EwGamesWon, dealing.NsGamesWon
            | Playing playing -> playing.EwGamesWon, playing.NsGamesWon
            | HandComplete complete -> complete.EwGamesWon, complete.NsGamesWon
            | GameComplete complete -> complete.EwGamesWon, complete.NsGamesWon
            | Error _ -> failwith "Invalid state"

module Model =

    let init () = Start
