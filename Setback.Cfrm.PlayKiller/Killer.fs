namespace Setback

open System
open System.IO

open PlayingCards

/// Logs messages to both console and file.
type Log() =

    static member WriteLine format =
        let writeline (message : string) =
            Console.WriteLine message
            use wtr = new IO.StreamWriter("Setback.log", true)
            wtr.WriteLine message
        Printf.ksprintf writeline format

/// Killer Setback integration
module Killer =

    let folder = "C:\Program Files\KSetback"

    /// Tokenizes the contents of the given file.
    let tokenize (fileInfo : FileInfo) =
        
        let mutable rdr : StreamReader = null
        while rdr = null do
            try
                rdr <- fileInfo.OpenText()
            with
                ex -> Threading.Thread.Sleep 0
        let tokens =
            rdr.ReadToEnd().Trim().Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                |> Seq.map (fun token -> Int32.Parse(token))
                |> Seq.toArray
        rdr.Dispose()
        tokens

    /// Receives a message from KS.
    let readMessage key =
        // Log.WriteLine "Read message %A" key
        let fileInfo = new FileInfo(System.IO.Path.Combine(folder, "KSetback.msg.master"))
        while not fileInfo.Exists do
            fileInfo.Refresh()
        let tokens = tokenize fileInfo
        if tokens[0] <> key then failwith (sprintf "Expected key: %d, received key: %d" key tokens[0])
        fileInfo.Delete()
        tokens[1], tokens[2], tokens[3], tokens[4]

    /// Sends a message to KS.
    let writeMessage key value =
        // Log.WriteLine "Write message %A" key
        let mutable wtr : StreamWriter = null
        while wtr = null do
            try
                wtr <- new IO.StreamWriter(System.IO.Path.Combine(folder, "KSetback.msg.slave"))
            with
                ex -> Threading.Thread.Sleep 0
        let message = sprintf "%d %d" key value
        wtr.Write message
        wtr.Dispose()

    /// Establishes initial communication with KS.
    let handshake () =
        Log.WriteLine "Killer Setback handshake"
        let fileInfo = new FileInfo(System.IO.Path.Combine(folder, "KSetback.msg.master"))
        if fileInfo.Exists then
            let tokens = tokenize fileInfo
            if tokens[0] <> 1 then
                fileInfo.Delete()
        readMessage 1 |> ignore
        writeMessage 101 0

    /// Converts the given KS card into a native card.
    let asCard cardNum =
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
    let asNum (card : Card) =
        let suitNum =
            match card.Suit with
                | Suit.Spades -> 0
                | Suit.Hearts -> 1
                | Suit.Clubs -> 2
                | Suit.Diamonds -> 3
                | _ -> failwith "Unexpected suit"
        let rankNum = (int card.Rank) - 2
        (rankNum * 4) + suitNum
