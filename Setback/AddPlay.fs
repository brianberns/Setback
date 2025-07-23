namespace Setback

open PlayingCards
open System.Collections.Immutable

/// This module is used to expand nodes during deal evaluation.
module AddPlay =

    /// Adds the given card to the deal
    let private add card (deal : OpenDeal) =

            // add card to underlying closed deal
        let closedDeal = deal.ClosedDeal.AddPlay card

            // trump has been established?
        if deal.TrumpOpt.IsSome then
            { deal with ClosedDeal = closedDeal }

            // establish trump
        else
            let trump = card.Suit
            let trumpCards =
                deal.Hands
                    |> Seq.collect id
                    |> Seq.where (fun card -> card.Suit = trump)
                    |> Seq.toArray
            let highTrump = trumpCards |> Seq.max
            let lowTrump = trumpCards |> Seq.min
            let jackTrumpOpt = trumpCards |> Seq.tryFind (fun card -> card.Rank = Rank.Jack)
            {
                deal with
                    ClosedDeal = closedDeal
                    HighTrumpOpt = Some highTrump
                    LowTrumpOpt = Some lowTrump
                    JackTrumpOpt = jackTrumpOpt
            }

    /// Increments the given team's score.
    let private incrTeam team n (score : Score) =
        if n <> 0 then
            score.WithPoints(team, score.[team] + n)
        else
            score

    /// Tests whether to increment the given count for the given point.
    let private incrTest count (pointsAwarded : ImmutableArray<bool>) (point : MatchPoint) test =
        if pointsAwarded.[int point] then
            (count, pointsAwarded, None)
        else
            match test() with
                | Some team -> (count + 1, pointsAwarded.SetItem(int point, true), Some team)
                | None -> (count, pointsAwarded, None)

    /// Increments match points for the given team.
    let private incrMatchPoints winTeam bidTeam count (deal : OpenDeal) =
        if deal.IsSet && (winTeam = bidTeam) then
            deal.MatchPoints   // don't award points to a team that's set
        else
            deal.MatchPoints |> incrTeam winTeam count

    /// Answers the team that holds the given card, if any.
    let private getTeam card (deal : OpenDeal) =
        Enum.getValues<Seat>
            |> Seq.tryFind (fun seat ->
                let cards = deal.Hands.[int seat]
                cards |> Seq.exists ((=) card))
            |> Option.map (fun seat -> deal.TeamMap.[int seat])

    /// Awards high point to the team that holds it.
    let private awardHighPoint bidTeam (deal : OpenDeal) =

            // find team
        let count, pointsAwarded, highTeamOpt =
            incrTest 0 deal.PointsAwarded MatchPoint.High (fun () ->
                getTeam deal.HighTrumpOpt.Value deal)

            // award point
        match highTeamOpt with
            | Some highTeam ->
                {
                    deal with
                        MatchPoints = deal |> incrMatchPoints highTeam bidTeam count
                        PointsAwarded = pointsAwarded
                }
            | None -> deal

    /// Awards points for cards taken on a trick by the given team.
    let private awardTrickPointsRaw cards trickTeam bidTeam (deal : OpenDeal) =

            // was the given card taken on this trick?
        let taken card =
            if cards |> List.exists ((=) card) then
                Some trickTeam
            else
                None

            // override incrementer for simplicity
        let incrTest count pointsAwarded point test =
            let count, pointsAwarded, _ = incrTest count pointsAwarded point test
            (count, pointsAwarded)

            // award high point?
        let incrHigh count pointsAwarded =
            incrTest count pointsAwarded MatchPoint.High (fun () ->
                taken deal.HighTrumpOpt.Value)

            // award low point?
        let incrLow count pointsAwarded =
            incrTest count pointsAwarded MatchPoint.Low (fun () ->
                taken deal.LowTrumpOpt.Value)

            // award jack point?
        let incrJack count pointsAwarded =
            incrTest count pointsAwarded MatchPoint.Jack (fun () ->
                deal.JackTrumpOpt
                    |> Option.map taken
                    |> Option.flatten)

            // award game point?
        let incrGame count pointsAwarded =
            incrTest count pointsAwarded MatchPoint.Game (fun () ->
                let threshold = deal.GamePointsTotal / 2   // assume only two teams
                if (deal.GamePoints.Points |> Seq.max) > threshold then
                    Some trickTeam
                else
                    None)

            // increment score of team that won this trick
        let count, pointsAwarded =
            (0, deal.PointsAwarded)
                ||> incrHigh
                ||> incrLow
                ||> incrJack
                ||> incrGame
        {
            deal with
                MatchPoints = deal |> incrMatchPoints trickTeam bidTeam count
                PointsAwarded = pointsAwarded
        }

    /// Applies the winning bid to the given team.
    let private applyBid bid bidTeam (deal : OpenDeal) =

        let bidPoints = int bid

            // bidding team was set?
        let isSet =
            let totPoints = deal.TotalMatchPoints
            let proPoints = deal.MatchPoints.[bidTeam]
            let conPoints = Seq.sum deal.MatchPoints.Points - proPoints
            if totPoints - conPoints < bidPoints then true                                          // opponents have already ensured that bidders are set
            elif proPoints < bidPoints && deal.Tricks.Length = OpenDeal.numCardsPerHand then true   // deal is over and bidders failed
            else false

            // if so, deduct bid from their score (erasing any points they may have won on this trick)
        let matchPoints =
            if isSet then
                deal.MatchPoints.WithPoints(bidTeam, -bidPoints)
            else
                deal.MatchPoints

        {
            deal with
                MatchPoints = matchPoints
                IsSet = isSet
        }

    /// Awards points taken on the given trick.
    let private awardTrickPoints bid bidTeam (deal : OpenDeal) =
   
            // prepare to process latest trick
        let trick = deal.Tricks.Head
        let cards = trick.Plays |> List.map snd
        let trickTeam = deal.TeamMap.[int trick.WinnerSeat]

            // tally game points won on this trick
        let deal =
            let gamePoints =
                let points = cards |> List.sumBy (fun card -> card.Rank.GamePoints)
                deal.GamePoints |> incrTeam trickTeam points
            { deal with GamePoints = gamePoints }

            // award match points won on this trick
        let deal = awardTrickPointsRaw cards trickTeam bidTeam deal

            // has bidding team been set?
        let deal =
            if deal.IsSet then
                deal
            else
                applyBid bid bidTeam deal

        deal

    /// Awards points that can be logically projected.
    let awardProjectedPoints (deal : OpenDeal) =

        assert (deal.Tricks.Head.NumPlays = Seat.numSeats)

            // get auction results
        let bidTeam =
            let bidder = deal.HighBidderOpt.Value
            deal.TeamMap.[int bidder]

            // prepare to award a point
        let award card point deal test =

                // has point been won?
            let count, pointsAwarded, winTeamOpt =
                incrTest 0 deal.PointsAwarded point (fun () ->
                    match getTeam card deal with
                        | Some proTeam ->
                            let conTrump =
                                Enum.getValues<Seat>
                                    |> Seq.where (fun seat ->
                                        let team = deal.TeamMap.[int seat]
                                        team <> proTeam)
                                    |> Seq.collect (fun seat ->
                                        deal.UnplayedCards seat
                                            |> Seq.where (fun card -> card.Suit = deal.Trump))
                            if test conTrump then
                                Some proTeam
                            else
                                None
                        | None -> None)

                // if so, award point to winning team
            match winTeamOpt with
                | Some winTeam ->
                    {
                        deal with
                            MatchPoints = deal |> incrMatchPoints winTeam bidTeam count
                            PointsAwarded = pointsAwarded
                    }
                | None -> deal

            // award low point if opponents have no trump
        let deal =
            award deal.LowTrumpOpt.Value MatchPoint.Low deal (fun trumpCards ->
                trumpCards |> Seq.isEmpty)

            // award jack point if opponents have no AKQ of trump (and jack exists)
        match deal.JackTrumpOpt with
            | Some jackTrump ->
                award jackTrump MatchPoint.Jack deal (fun trumpCards ->
                    trumpCards |> Seq.forall (fun card -> card.Rank < Rank.Jack))
            | None -> deal

    /// Answers a new deal with the next player's given discard.
    let addPlay card (deal : OpenDeal) =

#if DEBUG
        if deal.LegalPlays |> Seq.tryFind ((=) card) = None then
            failwith (sprintf "%A: Not a legal play (expected %A)" card (deal.LegalPlays |> Seq.map _.ToString() |> String.concat ", "))
#endif
            // get auction results
        let getBidTeam () =
            let bidder = deal.HighBidderOpt.Value
            deal.TeamMap.[int bidder]

            // add the card to the deal
        let isFirst = deal.Tricks.IsEmpty
        let deal = add card deal

            // award high when first card is played
        if isFirst then
            awardHighPoint (getBidTeam()) deal

            // award points at end of trick
        elif deal.Tricks.Head.NumPlays = Seat.numSeats then
            awardTrickPoints deal.HighBid (getBidTeam()) deal

            // no change
        else
            deal

[<AutoOpen>]
module AddPlayExt =
    type OpenDeal with
        member deal.AddPlay card = deal |> AddPlay.addPlay card
