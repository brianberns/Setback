namespace Setback.Model

open MathNet.Numerics.LinearAlgebra

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

/// Encoded value for input to a model.
type Encoding = bool[]

module Encoding =

    /// Encodes the given (card, value) pairs as a
    /// vector in the deck size.
    let inline encodeCardValues pairs =
        let valueMap =
            pairs
                |> Seq.map (fun (card, value) ->
                    Card.toIndex card, value)
                |> Map
        [|
            for index = 0 to Card.numCards - 1 do
                valueMap
                    |> Map.tryFind index
                    |> Option.defaultValue
                        LanguagePrimitives.GenericZero   // encode to input type
        |]

    /// Encodes the given cards as a multi-hot vector
    /// in the deck size.
    let encodeCards cards =
        let flags = Array.zeroCreate Card.numCards
        for index in Seq.map Card.toIndex cards do
            flags[index] <- true   // use mutation for speed
        flags

    /// Encodes cards played in the given tricks as multi-
    /// hot vectors for each player.
    let encodePlays player tricks =
        let seatPlayMap =
            Array.init Seat.numSeats (fun _ ->
                ResizeArray(Setback.numCardsPerHand))
        for trick in tricks do        
            for seat, card in Trick.plays trick do
                seatPlayMap[int seat].Add(card)
        [|
            for seat in Seat.cycle player do
                yield! encodeCards seatPlayMap[int seat]
        |]

    /// Encodes each card in the given current trick as
    /// a one-hot vector in the deck size and concatenates
    /// those vectors.
    let encodeTrick trickOpt =
        let cards =
            trickOpt
                |> Option.map (_.Cards >> Seq.toArray)
                |> Option.defaultValue Array.empty
        assert(cards.Length < Seat.numSeats)
        [|
            for iCard = 0 to Seat.numSeats - 2 do
                yield!
                    if iCard < cards.Length then
                        Some cards[cards.Length - 1 - iCard]   // unreverse into chronological order
                    else None
                    |> Option.toArray
                    |> encodeCards
        |]

    /// Encodes the given voids as a multi-hot vector in the
    /// number of suits times the number of other seats.
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

    /// Determines the number of deal points needed by each team
    /// to win a game with the given score.
    let toNeeds usTeam gameScore =

        let winThreshold =
            seq {
                for point in gameScore.Points do
                    yield point + 1
                yield Setback.winThreshold
            } |> Seq.max

        assert(Setback.numTeams = 2)
        let themTeam =
            if usTeam = Team.NorthSouth then Team.EastWest
            else Team.NorthSouth
        winThreshold - gameScore[usTeam],  // "us" need
        winThreshold - gameScore[themTeam]

    /// Encodes the given need as a "thermomenter".
    let encodeNeed need =
        assert(need > 0)
        let cutoff = Setback.numDealPoints + 1
        let count = (min cutoff need) - 1
        assert(count >= 0 && count < cutoff)   // 0-4
        Array.init (cutoff - 1) (fun i ->
            i < count)

    /// Encodes the given game score as thermometers.
    let encodeGameScore player gameScore =
        let usNeed, themNeed =
            toNeeds (Team.ofSeat player) gameScore
        [|
            yield! encodeNeed usNeed
            yield! encodeNeed themNeed
        |]

    /// Total encoded length of an info set.
    let encodedLength =
        Card.numCards                                         // current player's hand
            + Seat.numSeats * Card.numCards                   // cards previously played by each player
            + (Seat.numSeats - 1) * Card.numCards             // current trick
            + (Seat.numSeats - 1) * Suit.numSuits             // voids
            + Seat.numSeats                                   // deal score

    /// Encodes the given info set as a vector.
    let encode infoSet : Encoding =
        let flags =
            [|
                yield! encodeCards infoSet.Hand             // current player's hand
                yield! encodePlays                          // cards previously played by each player
                    infoSet.Player infoSet.Deal.CompletedTricks
                yield! encodeTrick                          // current trick
                    infoSet.Deal.CurrentTrickOpt
                yield! encodeVoids                          // voids
                    infoSet.Player infoSet.Deal.Voids
                yield! encodeGameScore                          // deal score
                    infoSet.Player infoSet.Deal.Score
            |]
        assert(flags.Length = encodedLength)
        flags

    /// Converts the given encoding to an array of float32.
    let toFloat32Array (encoding : Encoding) =
        encoding
            |> Array.map (fun flag ->
                if flag then 1f else 0f)
