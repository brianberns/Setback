namespace Setback.Cfrm.PlaySelf

open System
open System.Collections.Immutable
open System.Runtime
open System.Text

open PlayingCards
open Setback
open Setback.Cfrm

module Program =

    let toAbstractAuction auction =
        (AbstractAuction.initial, Auction.playerBids auction)
            ||> Seq.fold (fun absAuction (_, bid) ->
                AbstractAuction.addBid bid absAuction)

    let toAbstractPlayout absAuction playout =
        (AbstractPlayout.create absAuction.HighBid, Playout.tricks playout)
            ||> Seq.fold (fun absPlayout trick ->
                (absPlayout, Trick.plays trick)
                    ||> Seq.fold (fun absPlayout (_, card) ->
                        AbstractPlayout.addPlay card absPlayout))

    let toAbstractClosedDeal (deal : ClosedDeal) =
        let absAuction = toAbstractAuction deal.Auction
        let absPlayoutOpt =
            deal.PlayoutOpt
                |> Option.map (toAbstractPlayout absAuction)
        {
            Auction = absAuction
            PlayoutOpt = absPlayoutOpt
        }

    let toAbstractOpenDeal (hand : Hand) deal =
        let absClosedDeal = toAbstractClosedDeal deal
        let unplayedCards =
            ImmutableArray.CreateRange [|
                for seat in Seat.cycle deal.Dealer do
                    if seat = ClosedDeal.currentPlayer deal then
                        hand
                    else Set.empty
            |]
        let handLowTrumpRankOpts =
            ImmutableArray.CreateRange [|
                for seat in Seat.cycle deal.Dealer do
                    if seat = ClosedDeal.currentPlayer deal then
                        option {
                            let! playout = deal.PlayoutOpt
                            let! trump = playout.TrumpOpt
                            return! hand
                                |> Seq.where (fun card ->
                                    card.Suit = trump)
                                |> Seq.map (fun card ->
                                    card.Rank)
                                |> Seq.tryMin
                        }
                    else None
            |]
        {
            ClosedDeal = absClosedDeal
            UnplayedCards = unplayedCards
            HandLowTrumpRankOpts = handLowTrumpRankOpts
            HighTrumpOpt = None
            LowTrumpOpt = None
            JackTrumpOpt = None
            TotalGamePoints = 0
        }

    let getPlayer cfrmPlayer =
        let act infoSet =
            let absOpenDeal =
                toAbstractOpenDeal infoSet.Hand infoSet.Deal
            let absScore =
                let dealerTeam = Team.ofSeat infoSet.Deal.Dealer
                let otherTeam = Team.ofSeat (Seat.incr 1 infoSet.Deal.Dealer)
                AbstractScore [|
                    infoSet.GameScore[dealerTeam]
                    infoSet.GameScore[otherTeam]
                |]
            match infoSet.LegalActions[0] with
                | Choice1Of2 _ ->
                    cfrmPlayer.MakeBid absScore absOpenDeal
                        |> Choice1Of2
                | Choice2Of2 _ ->
                    cfrmPlayer.MakePlay absScore absOpenDeal
                        |> Choice2Of2
        { Act = act }

    [<EntryPoint>]
    let main argv =

        Console.OutputEncoding <- Encoding.UTF8
        printfn $"Server garbage collection: {GCSettings.IsServerGC}"

        let champion = DatabasePlayer.player "Champion.db" |> getPlayer
        let challenger = DatabasePlayer.player "Challenger.db" |> getPlayer
        Tournament.run 10000 champion challenger
            |> printfn "%A"

        0
