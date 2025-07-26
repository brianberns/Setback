namespace Setback

open PlayingCards

/// Possible points available in a Setback deal.
///
/// The terminology here gets quite confusing. We use the term "deal
/// point" to distinguish these points from "game points". Whichever team
/// gets the most game points in a deal wins the "Game" deal point for
/// that deal.
type DealPoint =
    | High = 0   // highest dealt trump
    | Low  = 1   // lowest dealt trump
    | Jack = 2   // jack of trump (if dealt)
    | Game = 3   // AKQJT of all suits

/// A deal is a round of play within a game. A closed deal is the
/// "public" view of a deal, so it contains no information about
/// how unplayed cards are distributed among the players.
type ClosedDeal =
    {
        Auction : Auction
        PlayoutOpt : Option<Playout>
    }

module ClosedDeal =

    /// Creates a new deal.
    let create dealer =
        {
            Auction = Auction.create dealer
            PlayoutOpt = None
        }

    /// Answers the points won by each team in this deal.
    let getOutcome (deal : ClosedDeal) =

            // find all played trump cards
        let fullTricks =
            deal.Tricks
                |> Seq.where (fun trick ->
                    trick.NumPlays = Seat.numSeats)
        let winner (trick : Trick) =
            deal.TeamMap[trick.Winner |> fst |> int]
        let trumpPairs =
            fullTricks
                |> Seq.collect (fun trick ->
                    trick.Plays
                        |> Seq.map (fun (_, card) ->
                            winner trick, card)
                        |> Seq.where (fun (_, card) ->
                            card.Suit = deal.Trump))
                |> Seq.toArray

            // which team won high/low trump?
        let highTeamOpt =
            if trumpPairs.Length > 0 then
                Some (trumpPairs |> Seq.maxBy snd |> fst)
            else
                None
        let lowTeamOpt =
            if trumpPairs.Length > 0 then
                Some (trumpPairs |> Seq.minBy snd |> fst)
            else
                None

            // which team won Jack of trump, if any?
        let jackTeamOpt =
            trumpPairs
                |> Seq.tryFind (fun (team, card) -> card.Rank = Rank.Jack)
                |> Option.map fst

            // did one team gather the most game points?
        let gameMap =
            fullTricks
                |> Seq.collect (fun trick ->
                    trick.Plays
                        |> Seq.map (fun (_, card) ->
                            trick, card))
                |> Seq.map (fun (trick, card) ->
                    winner trick, card.Rank.GamePoints)
                |> Seq.groupBy fst
                |> Seq.map (fun (team, pairs) ->
                    let count = pairs |> Seq.sumBy snd
                    team, count)
                |> Seq.toArray
        let maxCount =
            if gameMap.Length > 0 then
                gameMap
                    |> Seq.map snd
                    |> Seq.max
            else
                0
        let maxTeams =
            gameMap
                |> Seq.where (fun (_, count) ->
                    count = maxCount)
                |> Seq.map fst
                |> Seq.toList
        let gameTeamOpt =
            match maxTeams with   // point not awarded in case of tie
                | maxTeam :: [] -> Some maxTeam
                | _ -> None

        Map.empty
            |> Map.add DealPoint.High highTeamOpt
            |> Map.add DealPoint.Low lowTeamOpt
            |> Map.add DealPoint.Jack jackTeamOpt
            |> Map.add DealPoint.Game gameTeamOpt

    /// Answers the number of points won by the given team in the given deal.
    let getOutcomePoints team deal =
        deal |> getOutcome
            |> Seq.where (fun (KeyValue(_, teamOpt)) -> teamOpt = Some team)
            |> Seq.length

    let toString deal =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        if not this.Auction.IsEmpty then
            writeline ""
            for (seat, bid) in List.rev this.Auction do
                writeline (sprintf "%-5s: %A" (seat.ToString()) bid)

        if not this.Tricks.IsEmpty then
            writeline ""
            this.Tricks
                |> List.rev
                |> List.iteri (fun iTrick trick ->
                    write (sprintf "%A: " (iTrick + 1))
                    let sTrick =
                        trick
                            |> Trick.plays
                            |> Seq.map (fun (seat, card) ->
                                sprintf "%c:%A" seat.Char card)
                            |> String.concat " "
                    writeline (sprintf "%s" sTrick))

        sb.ToString()
