namespace Setback.PlayModel

open System

open PlayingCards
open Setback
open Setback.Model

module Program =

    let run () =

        use model =
            new AdvantageModel(1200, 5, 0.0, TorchSharp.torch.CPU)
        model.load("AdvantageModel.pt") |> ignore
        model.eval()

        // Thread-safe player: uses Random.Shared so Act can be
        // called from multiple threads concurrently.
        let act infoSet =
            let strategy =
                Strategy.getFromAdvantage model [| infoSet |]
                    |> Array.exactlyOne
            Vector.sample Random.Shared strategy
                |> Array.get infoSet.LegalActions
        let player = { Act = act }

        // "Low" non-club cards (suits S, D, H; ranks 2-10).
        let lowCards =
            [|
                for suit in [ Suit.Spades; Suit.Diamonds; Suit.Hearts ] do
                    for rankNum = 2 to 10 do
                        Card(enum<Rank> rankNum, suit)
            |]

        let queenClubs = Card.fromString "QC"
        let allCards = set Card.allCards
        let dealer = Seat.North

        // Plays a single deal to completion using the model for
        // every decision (modeled on Game.playGame, but stops at
        // end of deal). East's first bid is forced to firstBid.
        let playDeal handMap firstBid =
            let game =
                {
                    Deal =
                        OpenDeal.fromHands dealer handMap
                            |> OpenDeal.addBid firstBid
                    Score = Score.zero
                }
            let rec loop (game : Game) =
                if OpenDeal.isComplete game.Deal then
                    ClosedDeal.getDealScore game.Deal.ClosedDeal
                else
                    let infoSet = Game.currentInfoSet game
                    let action =
                        match Seq.tryExactlyOne infoSet.LegalActions with
                            | Some action -> action
                            | None -> player.Act infoSet
                    loop (Game.addAction action game)
            loop game

        let rng = Random()
        let nDealPairs = 100_000

        // Pre-generate all hand maps serially (rng is not
        // thread-safe).
        let handMaps =
            Array.init nDealPairs (fun _ ->

                    // East's hand: singleton Q♣ plus 5 random low cards
                let eastHand =
                    set [
                        queenClubs
                        yield! Array.randomShuffleWith rng lowCards
                            |> Array.take 5
                    ]

                    // Deal 6 cards each to N, S, W from the remaining cards
                let others =
                    Set.difference allCards eastHand
                        |> Set.toArray
                        |> Array.randomShuffleWith rng
                Map [
                    Seat.East,  eastHand
                    Seat.North, set others[ 0 ..  5]
                    Seat.South, set others[ 6 .. 11]
                    Seat.West,  set others[12 .. 17]
                ])

        // Play deal pairs in parallel.
        let results =
            handMaps
                |> Array.Parallel.map (fun handMap ->
                    let scorePass = playDeal handMap Bid.Pass
                    let scoreTwo  = playDeal handMap Bid.Two
                    let payoff (score : Score) =
                        score[Team.EastWest] - score[Team.NorthSouth]
                    payoff scorePass, payoff scoreTwo)

        let avgPass = results |> Array.averageBy (fst >> float)
        let avgTwo  = results |> Array.averageBy (snd >> float)
        let avgDiff =
            results
                |> Array.averageBy (fun (p, t) -> float (t - p))
        let nTwoBetter =
            results
                |> Array.filter (fun (p, t) -> t > p)
                |> Array.length
        let nTwoWorse =
            results
                |> Array.filter (fun (p, t) -> t < p)
                |> Array.length
        let nTie =
            results
                |> Array.filter (fun (p, t) -> t = p)
                |> Array.length

        printfn $"Deal pairs: {nDealPairs}"
        printfn $"Avg payoff (E+W minus N+S) when East passes: {avgPass:F4}"
        printfn $"Avg payoff (E+W minus N+S) when East bids 2: {avgTwo:F4}"
        printfn $"Avg difference (Two - Pass):                 {avgDiff:F4}"
        printfn $"Two better than Pass: {nTwoBetter}"
        printfn $"Two worse  than Pass: {nTwoWorse}"
        printfn $"Tie:                  {nTie}"

    do
        Console.OutputEncoding <- Text.Encoding.UTF8
        run ()
