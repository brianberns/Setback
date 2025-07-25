namespace Setback

open System.Collections.Immutable

open PlayingCards

/// Possible points(*) available in a Setback deal.
///
/// *The terminology here gets quite confusing. We use the term "match
/// point" to distinguish these points from "game points". Whichever team
/// gets the most game points in a deal wins the "Game" match point for
/// that deal.
type MatchPoint =
    | High = 0   // highest dealt trump
    | Low  = 1   // lowest dealt trump
    | Jack = 2   // jack of trump (if dealt)
    | Game = 3   // AKQJT of all suits

/// A deal is a round of play within a game, consisting of the
/// following phases:
///
/// * Deal: Distribution of cards from the deck to the players
/// * Auction: Bids by players on their hands
/// * Playout: Discards by players from their hands, grouped into tricks
///
/// A closed deal contains no information about unplayed cards,
/// which are kept private by each player.
[<StructuredFormatDisplay("{String}")>]
type ClosedDeal =
    {
            // player who dealt this hand
        Dealer : Seat

            // each player's bid, in reverse chronological order (dealer bids last)
        Auction : List<Seat * Bid>

            // auction winner
        HighBidderOpt : Option<Seat>
        HighBid : Bid

            // trump is determined by first card played
        TrumpOpt : Option<Suit>

            // each player's played cards, in reverse chronological order
        Tricks : List<Trick>

            // cards played in this trick
        CardsPlayed : ImmutableArray<bool>(*[Card.ToInt]*)   // tracks all 52 cards

            // each player's void suits
        Voids : ImmutableArray<bool>(*[Seat, Suit]*)
    }

    // Trump is determined by first card played
    member deal.Trump =
        match deal.TrumpOpt with
            | Some trump -> trump
            | None -> failwith "No trump yet"

    member this.String =

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

module ClosedDeal =

    /// Creates a new deal.
    let create teams dealer : ClosedDeal =

        let teamMap =
            teams
                |> Seq.collect (fun team ->
                    team.Seats
                        |> Seq.map (fun seat ->
                            seat, team))
                |> Seq.sortBy fst
                |> Seq.map snd
                |> Seq.toArray

        {
            Teams = teams
            TeamMap = teamMap
            Dealer = dealer
            Auction = []
            HighBidderOpt = None
            HighBid = Bid.Pass
            TrumpOpt = None
            Tricks = []
            CardsPlayed = ImmutableArray.Empty
            Voids = ImmutableArray.ZeroCreate(Seat.numSeats * Suit.numSuits)
        }

    /// Whose turn is it to bid next?
    let nextBidder deal =

        let lastBidder =
            match deal.Auction with
                | (seat, _) :: _ ->
                    if (seat = deal.Dealer) then failwith "Auction is over"
                    seat
                | [] -> deal.Dealer
        lastBidder.Next

    /// What are the allowed bids in the current situation?
    let legalBids deal =

        let bid = deal.HighBid
        seq {
            yield Bid.Pass
            if bid < Bid.Two then yield Bid.Two
            if bid < Bid.Three then yield Bid.Three
            if bid < Bid.Four then yield Bid.Four
            elif deal.Auction.Length = Seat.numSeats - 1 then
                yield Bid.Four   // dealer can steal 4-bid
        }

    /// Answers a new deal with the next player's given bid.
    let addBid bid deal =

        if deal |> legalBids |> Seq.tryFind ((=) bid) = None then
            failwith "Not a legal bid"

            // is this bid currently winning the auction?
        let bidder = nextBidder deal
        let highBidderOpt, highBid =
            if bid = Bid.Pass then
                deal.HighBidderOpt, deal.HighBid
            else
                Some bidder, bid

        {
            deal with
                Auction = (bidder, bid) :: deal.Auction
                HighBidderOpt = highBidderOpt
                HighBid = highBid
        }

    /// Current trick in the given deal.
    let currentTrick deal =
        match deal.CurrentTrickOpt with
            | Some trick -> trick
            | None -> failwith "No current trick"

    /// Current player in the given deal, once the exchange
    let currentPlayer deal =
        deal
            |> currentTrick
            |> Trick.currentPlayer
        deal.Tricks
            |> List.tryHead
            |> Option.map (fun trick ->

                    // winner of previous trick leads
                if Trick.isComplete trick then
                    None, trick.WinnerSeat

                    // current trick continues
                else
                    let prevSeat, _ = trick.Plays.Head
                    Some trick, prevSeat.Next)
            |> Option.defaultWith (fun () ->

                    // auction winner leads first trick
                None, deal.HighBidderOpt.Value)

    /// What cards is the current player allowed to play? Note that the
    /// deal is not otherwise aware of the player's hand.
    let legalPlays hand (deal : ClosedDeal) =

        let trickOpt, _ = deal |> nextPlayer
        match trickOpt with

                // continue current trick
            | Some trick ->
                let isTrump (card : Card) =
                    card.Suit = deal.Trump
                let isFollowSuit (card : Card) =
                    card.Suit = trick.SuitLed
                if hand |> Seq.exists isFollowSuit then      // player can follow suit?
                    hand |> Seq.where (fun card ->
                        isTrump card || isFollowSuit card)   // player can always trump in
                else
                    hand

                // start a new trick
            | None -> hand

    /// Computes linear index for is-void flag of the given suit for the given player.
    let private voidIndex (seat : Seat) (suit : Suit) =
        (int seat * Suit.numSuits) + (int suit)

    /// Indicates whether the given player is void in the given suit.
    let isVoid seat suit deal =
        let index = voidIndex seat suit
        deal.Voids.[index]

    /// Sets the given seat to be void in the given suit.
    let setVoid seat suit (voids : ImmutableArray<bool>) =
        let index = voidIndex seat suit
        voids.SetItem(index, true)

    /// Answers a new deal with the next player's given discard.
    let addPlay (card : Card) deal =

            // mark this card as played
        let cardsPlayed =
            deal.CardsPlayed.SetItem(Card.toIndex card, true)

            // compute trump suit
        let trump =
            match deal.TrumpOpt with
                | Some trump -> trump
                | None -> card.Suit

            // compute resulting trick
        let trickOpt, seat = deal |> nextPlayer
        let trick, tricks =
            match trickOpt with
                    
                    // continue current trick
                | Some trick ->
                    let newTrick = trick.Add(seat, card)
                    newTrick, newTrick :: deal.Tricks.Tail

                    // start a new trick
                | None ->
                    let newTrick = Trick.create trump seat card
                    newTrick, newTrick :: deal.Tricks

            // player is void in suit led?
        let voids =
            if card.Suit = trump then
                deal.Voids
            else
                let index = voidIndex seat trick.SuitLed
                if card.Suit = trick.SuitLed then
                    assert(not deal.Voids.[index])
                    deal.Voids
                else
                    deal.Voids.SetItem(index, true)

        {
            deal with
                TrumpOpt = Some trump
                Tricks = tricks
                CardsPlayed = cardsPlayed
                Voids = voids
        }

    /// Answers the points won by each team in this deal.
    let getOutcome (deal : ClosedDeal) =

            // find all played trump cards
        let fullTricks =
            deal.Tricks
                |> Seq.where (fun trick ->
                    trick.NumPlays = Seat.numSeats)
        let winner (trick : Trick) =
            deal.TeamMap.[trick.Winner |> fst |> int]
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
            |> Map.add MatchPoint.High highTeamOpt
            |> Map.add MatchPoint.Low lowTeamOpt
            |> Map.add MatchPoint.Jack jackTeamOpt
            |> Map.add MatchPoint.Game gameTeamOpt

    /// Answers the number of points won by the given team in the given deal.
    let getOutcomePoints team deal =
        deal |> getOutcome
            |> Seq.where (fun (KeyValue(_, teamOpt)) -> teamOpt = Some team)
            |> Seq.length

[<AutoOpen>]
module ClosedDealExt =
    type ClosedDeal with
        member deal.NextBidder = deal |> ClosedDeal.nextBidder
        member deal.LegalBids = deal |> ClosedDeal.legalBids
        member deal.AddBid(bid) = deal |> ClosedDeal.addBid bid
        member deal.NextPlayer = deal |> ClosedDeal.nextPlayer
        member deal.LegalPlays(hand) = deal |> ClosedDeal.legalPlays hand
        member deal.AddPlay(card) = deal |> ClosedDeal.addPlay card
