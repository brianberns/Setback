namespace Setback.Model

open PlayingCards
open Setback

module Card =

    /// Rank of lowest card in the deck.
    let private minRank =
        Seq.min Enum.getValues<Rank>

    /// Converts the given card to an integer, 0..N-1,
    /// where N is number of cards in the deck.
    let toIndex (card : Card) =
        let index =
            (int card.Suit * Rank.numRanks)
                + int card.Rank - int minRank
        assert(index >= 0)
        assert(index < Card.numCards)
        index

module Action =

    /// Converts the given action to an integer, 0..N-1,
    /// where N is number of bids + cards in the deck.
    let toIndex (action : Action) =
        match action with
            | Choice1Of2 bid ->
                int bid
            | Choice2Of2 card ->
                Bid.numBids + Card.toIndex card

/// Encoded value for input to a model.
type Encoding = bool[]

module Encoding =

    /// Decodes cards from the given flags.
    let decodeCards flags =
        assert(Array.length flags = Card.numCards)
        seq {
            for iFlag = 0 to flags.Length - 1 do
                if flags[iFlag] then
                    Card.allCards[iFlag]
        }

    /// Encodes the given cards as a multi-hot vector in the
    /// deck size.
    let encodeCards cards =
        let flags = Array.zeroCreate Card.numCards
        for index in Seq.map Card.toIndex cards do
            flags[index] <- true   // use mutation for speed
        assert(set (decodeCards flags) = set cards)
        flags

    /// Decodes an optional seat from the given flags.
    let decodeSeat player flags =
        assert(Array.length flags = Seat.numSeats)
        let allSeats = Seat.cycle player |> Seq.toArray
        let seats =
            [|
                for iFlag = 0 to flags.Length - 1 do
                    if flags[iFlag] then
                        allSeats[iFlag]
            |]
        assert(seats.Length <= 1)
        Array.tryExactlyOne seats

    /// Encodes the given seat as a one-hot vector in the number
    /// of seats, relative to the given player, or zero-hot if
    /// none.
    let encodeSeat player seatOpt =
        let flags =
            [|
                for seat in Seat.cycle player do
                    Some seat = seatOpt
            |]
        assert(decodeSeat player flags = seatOpt)
        flags

    /// Encodes the given bid as a one-hot vector in the number
    /// of bids, or zero-hot if none.
    let encodeBid bidOpt =
        [|
            for bid in Enum.getValues<Bid> do
                Some bid = bidOpt
        |]

    /// Encodes the given auction (which might be in progress or
    /// have completed), relative to the given player.
    let encodeAuction player auction =
        [|
                // dealer
            yield! encodeSeat player (Some auction.Dealer)

                // each player's bid in chronological order
            let bids = Seq.toArray auction.Bids
            for iBid = 0 to Seat.numSeats - 1 do
                yield!
                    if iBid < bids.Length then
                        Some bids[bids.Length - 1 - iBid]   // unreverse into chronological order
                    else None
                    |> encodeBid
        |]

    /// Encodes the given trick (which might be in progress, or
    /// have completed, or have not started), relative to the
    /// given player
    let encodeTrick player trickOpt =
        [|
                // trick leader
            yield! encodeSeat player
                (Option.map _.Leader trickOpt)

                // each player's card in chronological order
            let cards =
                trickOpt
                    |> Option.map (_.Cards >> Seq.toArray)
                    |> Option.defaultValue Array.empty
            for iCard = 0 to Seat.numSeats - 1 do
                yield!
                    if iCard < cards.Length then
                        Some cards[cards.Length - 1 - iCard]   // unreverse into chronological order
                    else None
                    |> Option.toArray
                    |> encodeCards
        |]

    /// Encoded length of a trick.
    let encodedTrickLength =
        Seat.numSeats                         // trick leader
            + Seat.numSeats * Card.numCards   // each card in trick

    /// Encodes the given voids as a multi-hot vector in the
    /// number of suits times the number of other seats,
    /// relative to the given player.
    let encodeVoids player voids =
        let flags =
            Array.zeroCreate ((Seat.numSeats - 1) * Suit.numSuits)
        for seat, suit in voids do
            if seat <> player then
                let suitOffset = (Seat.numSeats - 1) * int suit
                let seatOffset =
                    ((int seat - int player - 1) + Seat.numSeats)
                        % Seat.numSeats
                flags[suitOffset + seatOffset] <- true   // use mutation for speed
        flags

    /// Encodes the given playout (which might not have started),
    /// relative to the given player.
    let encodePlayout player playoutOpt =
        [|
                // tricks
            let tricks =
                playoutOpt
                    |> Option.map (Playout.tricks >> Seq.toArray)
                    |> Option.defaultValue Array.empty
            assert(tricks.Length < Setback.numCardsPerHand)   // no need to encode last trick
            for iTrick = 0 to Setback.numCardsPerHand - 2 do
                yield!
                    if iTrick < tricks.Length then
                        Some tricks[iTrick]   // already in chronological order
                    else None
                    |> encodeTrick player

                // voids
            let voids =
                playoutOpt
                    |> Option.map _.Voids
                    |> Option.defaultValue Set.empty
            yield! encodeVoids player voids
        |]

    /// Encoded length of game points for one team:
    ///    * 1111 -> team is 1 point from winning (e.g. has 10 points, or tied 11-11, etc.)
    ///    * 1110 -> team is 2 points from winning
    ///    * 1100 -> team is 3 points from winning
    ///    * 1000 -> team is 4 points from winning
    ///    * 0000 -> team if 5+ points form winning
    let encodedGamePointLength = Setback.numDealPoints

    /// Encodes the given game score as thermometers, relative to
    /// the given player.
    let encodeGameScore player (gameScore : Score) =
        assert(Setback.numTeams = 2)
        let usTeam = Team.ofSeat player
        let themTeam = 
            if usTeam = Team.NorthSouth then Team.EastWest
            else Team.NorthSouth
        [| usTeam; themTeam |]
            |> Array.collect (fun team ->
                let count =
                    match gameScore[team] with
                        | pt when pt >= 10 -> 4
                        | 9 -> 3
                        | 8 -> 2
                        | 7 -> 1
                        | _ -> 0
                assert(count <= encodedGamePointLength)
                Array.init encodedGamePointLength (fun i -> i < count))

    /// Total encoded length of an info set.
    let encodedLength =
        Card.numCards                                  // current player's hand
            + Seat.numSeats                            // dealer
            + Seat.numSeats * Bid.numBids              // each player's bid
            + (Setback.numCardsPerHand - 1)            // past, present, and future tricks
                * encodedTrickLength
            + (Seat.numSeats - 1) * Suit.numSuits      // voids
            + Team.numTeams * encodedGamePointLength   // game score

    /// Encodes the given info set as a vector.
    let encode infoSet : Encoding =
        let flags =
            [|
                yield! encodeCards infoSet.Hand
                yield! encodeAuction
                    infoSet.Player infoSet.Deal.Auction
                yield! encodePlayout
                    infoSet.Player infoSet.Deal.PlayoutOpt
                yield! encodeGameScore
                    infoSet.Player infoSet.GameScore
            |]
        assert(flags.Length = encodedLength)
        flags

    /// Encodes the given (action, value) pairs as a vector
    /// in the bid + deck size.
    let encodeActionValues pairs =
        let valueMap =
            pairs
                |> Seq.map (fun (action, value) ->
                    Action.toIndex action, value)
                |> Map
        [|
            let nActions = Bid.numBids + Card.numCards
            for index = 0 to nActions - 1 do
                valueMap
                    |> Map.tryFind index
                    |> Option.defaultValue 0f
        |]

    /// Converts the given encoding to an array of float32.
    let toFloat32Array (encoding : Encoding) =
        encoding
            |> Array.map (fun flag ->
                if flag then 1f else 0f)
