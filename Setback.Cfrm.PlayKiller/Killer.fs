namespace Setback

open System
open System.IO

open PlayingCards

/// Killer Setback integration
module Killer =

    let folder = @"C:\Program Files\KSetback"

    /// Tokenizes the contents of the given file.
    let tokenize (fileInfo : FileInfo) =
        use mutable rdr : StreamReader = null
        while rdr = null do
            try rdr <- fileInfo.OpenText()
            with _ -> Threading.Thread.Sleep 1
        rdr.ReadToEnd()
            .Trim()
            .Split(
                [|' '|],
                StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map Int32.Parse
            |> Seq.toArray

    /// Receives a message from KS.
    let readMessage () =
        let fileInfo = FileInfo(Path.Combine(folder, "KSetback.msg.master"))
        while not fileInfo.Exists do
            fileInfo.Refresh()
        let tokens = tokenize fileInfo
        assert(tokens.Length = 5)
        fileInfo.Delete()
        tokens

    /// Sends a message to KS.
    let writeMessage key value =
        use mutable wtr : StreamWriter = null
        while wtr = null do
            try wtr <- new StreamWriter(Path.Combine(folder, "KSetback.msg.slave"))
            with _ -> Threading.Thread.Sleep(0)
        let message =
            if key = 101 && false then   // new version of KSetback
                sprintf "%d %d\r\nBernsrite Setback" key value
            else
                sprintf "%d %d" key value
        wtr.Write message

    /// Converts the given KS card into a native card.
    let toCard cardNum =
        let suit =
            match cardNum % 4 with
                | 0 -> Suit.Spades
                | 1 -> Suit.Hearts
                | 2 -> Suit.Clubs
                | 3 -> Suit.Diamonds
                | _ -> failwith "Unexpected suit"
        let rank = enum<Rank> ((cardNum / 4) + 2)
        Card(rank, suit)

    /// Converts the given native card into a KS card.
    let toNum (card : Card) =
        let suitNum =
            match card.Suit with
                | Suit.Spades -> 0
                | Suit.Hearts -> 1
                | Suit.Clubs -> 2
                | Suit.Diamonds -> 3
                | _ -> failwith "Unexpected suit"
        let rankNum = (int card.Rank) - 2
        (rankNum * 4) + suitNum
