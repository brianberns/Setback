namespace Setback

open PlayingCards
open System.Collections.Immutable

/// An open deal contains each player's hand. This is used for two purposes:
/// * As the central authority when "hosting" a game of Setback
/// * As a predictive mechanism when evaluating a closed deal
[<StructuredFormatDisplay("{AsString}")>]
type OpenDeal =
    {
            // base deal
        ClosedDeal : ClosedDeal

            // each player's cards, by seat
        Hands : Card[][(*Seat*)]

            // highest trump card dealt
        HighTrumpOpt : Option<Card>

            // lowest trump card dealt
        LowTrumpOpt : Option<Card>

            // jack of trump was dealt?
        JackTrumpOpt: Option<Card>

            // running tally of AKQJT taken
        GamePoints : Score
        GamePointsTotal : int

            // running tally of high, low, jack, and game
        MatchPoints : Score<int>
        PointsAwarded : ImmutableArray<bool>

            // bidding team has been set?
        IsSet : bool
    }
        // poor man's inheritance
    member this.Dealer = this.ClosedDeal.Dealer
    member this.Auction = this.ClosedDeal.Auction
    member this.HighBidderOpt = this.ClosedDeal.HighBidderOpt
    member this.HighBid = this.ClosedDeal.HighBid
    member this.TrumpOpt = this.ClosedDeal.TrumpOpt
    member this.Trump = this.ClosedDeal.Trump
    member this.CardsPlayed = this.ClosedDeal.CardsPlayed
    member this.Tricks = this.ClosedDeal.Tricks
    member this.Teams = this.ClosedDeal.Teams
    member this.NextPlayer = this.ClosedDeal.NextPlayer
    member this.TeamMap = this.ClosedDeal.TeamMap
    member this.GetOutcome = this.ClosedDeal.GetOutcome
    member this.GetOutcomePoints = this.ClosedDeal.GetOutcomePoints

    member this.AsString =

        let sb = new System.Text.StringBuilder()
        let write (s : string) = sb.Append(s) |> ignore
        let writeline (s : string) = sb.AppendFormat("{0}\r\n", s) |> ignore

        writeline ""
        for seat in this.Dealer.Next.Cycle do
            let sHand = this.Hands.[int seat] |> Hand.toString
            writeline (sprintf "%-5s: %s" (seat.ToString()) sHand)

        write this.ClosedDeal.AsString

        let dumpScore teams (score : Score) =
            for team in teams do
                sb.AppendFormat("   {0}: {1}\r\n", team, score.[team]) |> ignore

        if not this.Tricks.IsEmpty then
            writeline ""
            writeline "Game points:"
            dumpScore this.Teams this.GamePoints
            writeline ""
            writeline "Match points:"
            dumpScore this.Teams this.MatchPoints

        sb.ToString()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OpenDeal =

    /// Number of cards dealt to each player.
    let numCardsPerHand = 6

    /// Creates a deal from the given hands.
    let fromHands teams dealer hands =
        {
            ClosedDeal = ClosedDeal.create teams dealer
            Hands = hands |> Seq.map (fun hand -> hand |> Seq.toArray) |> Seq.toArray   // allow flexible input
            HighTrumpOpt = None
            LowTrumpOpt = None
            JackTrumpOpt = None
            GamePoints = Score.zeroCreate teams
            GamePointsTotal =
                hands |> Seq.collect id |> Seq.sumBy (fun card -> card.Rank.GamePoints)
            MatchPoints = Score.zeroCreate teams
            PointsAwarded =
                let nPoints = Utility.allValues<MatchPoint> |> Seq.length
                Utility.zeroCreateImmutable(nPoints)
            IsSet = false
        }

    /// Deals cards from the given deck to each player.
    let fromDeck teams dealer deck =

        let numCardsPerGroup = 3
        assert (numCardsPerHand % numCardsPerGroup = 0)

        deck.Cards

                // number each card
            |> Seq.mapi (fun iCard card -> (iCard, card))

                // assign each card to a player
            |> Seq.groupBy (fun (iCard, card) ->
                ((int dealer) + (iCard / numCardsPerGroup) + 1) % Seat.numSeats)   // deal first group of cards to dealer's left

                // gather each player's cards
            |> Seq.map (fun (_, pairs) ->
                pairs
                    |> Seq.map snd
                    |> Seq.take numCardsPerHand)

                // create a deal from these hands
            |> fromHands teams dealer

    /// Total number of match points available in the given deal (either 3 or 4).
    let totalMatchPoints (deal : OpenDeal) =
        if deal.JackTrumpOpt.IsSome then 4 else 3

    /// Answers a new deal with the next player's given bid.
    let addBid bid deal =
        { deal with ClosedDeal = deal.ClosedDeal.AddBid bid }

    /// Answers the unplayed cards in the given player's hand.
    let unplayedCards seat (deal : OpenDeal) =
        deal.Hands.[int seat]
            |> Seq.where (fun card -> not (deal.CardsPlayed.GetFlag card.ToInt))

    /// Answers the number of cards played so far.
    let numCardsPlayed (deal : OpenDeal) =
        deal.Tricks |> Seq.sumBy (fun trick -> trick.NumPlays)

    /// What cards is the current player allowed to play?
    let legalPlays (deal : OpenDeal) =

            // get unplayed cards in player's hand
        let trickOpt, seat = deal.NextPlayer
        let cards = deal |> unplayedCards seat

        match trickOpt with

                // continue current trick
            | Some trick ->
                let isTrump card = (card.Suit = deal.Trump)
                let isFollowSuit card = (card.Suit = trick.SuitLed)
                if cards |> Seq.exists isFollowSuit then                                     // player can follow suit?
                    if deal.Trump = trick.SuitLed then
                        cards |> Seq.where (fun card -> isTrump card)                        // unroll for max performance
                    else
                        cards |> Seq.where (fun card -> isTrump card || isFollowSuit card)   // player can always trump in
                else
                    cards

                // start a new trick
            | None -> cards

[<AutoOpen>]
module OpenDealExt =
    type OpenDeal with
        member deal.NextBidder = deal.ClosedDeal.NextBidder
        member deal.LegalBids = deal.ClosedDeal.LegalBids
        member deal.AddBid bid = deal |> OpenDeal.addBid bid
        member deal.UnplayedCards seat = deal |> OpenDeal.unplayedCards seat
        member deal.NumCardsPlayed = deal |> OpenDeal.numCardsPlayed
        member deal.LegalPlays = deal |> OpenDeal.legalPlays
        member deal.TotalMatchPoints = deal |> OpenDeal.totalMatchPoints
