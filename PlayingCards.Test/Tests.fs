namespace PlayingCards.Test

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open PlayingCards

[<TestClass>]
type TestClass () =

    [<TestMethod>]
    member _.RandomShuffle() =
        let rng = Random(0)
        let decks =
            Array.init 100000 (fun _ -> Deck.shuffle rng)
        for iCard = 0 to Card.numCards - 1 do
            let counts =
                decks
                    |> Seq.map (fun deck -> deck.Cards[iCard])
                    |> Seq.groupBy id
                    |> Seq.map (fun (_, cards) -> cards |> Seq.length)
                    |> Seq.toArray
            Assert.AreEqual<int>(Card.numCards, counts.Length)
            let maxCount = Seq.max counts
            let minCount = Seq.min counts
            let ratio = (float decks.Length) / (float Card.numCards)
            let delta = 0.1
            Assert.IsTrue(float maxCount < (1.0 + delta) * ratio)
            Assert.IsTrue(float minCount > (1.0 - delta) * ratio)
