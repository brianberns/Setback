namespace Setback.Cfrm.LoadDatabase

open System
open System.Data
open System.Data.SQLite

open Cfrm

open Setback.Cfrm

module Program =

    /// Creates database schema.
    let createSchema conn =
        use cmd =
            new SQLiteCommand(
                "create table Strategy ( \
                    Key text primary key, \
                    ActionIndex integer not null \
                )",
                conn)
        cmd.ExecuteNonQuery() |> ignore

    /// Creates and connects to database.
    let connect dbFileName =

            // always create a new database (WARNING: overrides existing database, if any)
        SQLiteConnection.CreateFile(dbFileName)

            // open connection
        let connStr = sprintf "DataSource=%s;Version=3;" dbFileName
        let conn = new SQLiteConnection(connStr)
        conn.Open()

            // create schema
        createSchema conn

        conn

    /// Converts the given option to a database-safe value.
    let safeValue valueOpt =
        valueOpt
            |> Option.map (fun value -> value :> obj)
            |> Option.defaultValue (DBNull.Value :> _)

    /// Is a playout key?
    let isPlayout (key : string) =
        ".EH".Contains(key.[0])

    /// Creates and loads database.
    let load conn =

            // describe strategy files
        let profileDescs =
            [|
                "Baseline.strategy", isPlayout
                "Bootstrap.strategy", isPlayout >> not
            |]

            // enable bulk load
        use pragmaCmd =
            new SQLiteCommand(
                "PRAGMA journal_mode = OFF; \
                PRAGMA synchronous = OFF",
                conn)
        pragmaCmd.ExecuteNonQuery() |> ignore

            // prepare insert command
        use strategyCmd =
            new SQLiteCommand(
                "insert into Strategy (Key, ActionIndex) \
                values (@Key, @ActionIndex)",
                conn)
        let stratKeyParam = strategyCmd.Parameters.Add("Key", DbType.String)
        let stratActionIdxParam = strategyCmd.Parameters.Add("ActionIndex", DbType.Int32)

            // load each profile
        for (name, pred) in profileDescs do
            printfn $"{name}"
            let profile = StrategyProfile.Load(name)

                // insert profile's rows
            for (iStrategy, (key, probs)) in profile.Map |> Map.toSeq |> Seq.indexed do

                let strategyNum = iStrategy + 1
                if strategyNum % 100000 = 0 then
                    printfn $"{strategyNum}"

                if pred key then
                    stratKeyParam.Value <- key
                    stratActionIdxParam.Value <-
                        probs
                            |> Seq.indexed
                            |> Seq.maxBy snd
                            |> fst
                    let nRows = strategyCmd.ExecuteNonQuery()
                    assert(nRows = 1)

    [<EntryPoint>]
    let main argv =
        use conn = connect "Setback.db"
        load conn
        0
