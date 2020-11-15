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

    /// Creates and loads database.
    let load conn =

            // open strategy file
        let profile =
            StrategyProfile.Load("Baseline.strategy")

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

            // insert rows
        for (iRow, (key, probs)) in profile.Map |> Map.toSeq |> Seq.indexed do

            let rowNum = iRow + 1
            if rowNum % 100000 = 0 then
                printfn "Row: %d" rowNum

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
