namespace Core.DbConnection

open System
open System.Collections.Generic
open System.Data
open System.Data.Common
open System.Threading.Tasks
open Core.AppSettings
open Npgsql

type IDbConnection =
    abstract ExecuteSqlReader : sql : string -> queryParams : NpgsqlParameter IReadOnlyCollection -> DbDataReader Task
    abstract ExecuteSqlNonQuery : sql : string -> queryParams : NpgsqlParameter IReadOnlyCollection -> int32 Task
    abstract ExecuteSqlScalar : sql : string -> queryParams : NpgsqlParameter IReadOnlyCollection -> obj Task

type DbTransactionWrapper (tx : NpgsqlTransaction) =
    member _.CreateCommand (sql : string, queryParams : NpgsqlParameter IReadOnlyCollection) =
        let cmd = new NpgsqlCommand (sql, tx.Connection)
        cmd.Transaction <- tx
        cmd.Parameters.AddRange (queryParams |> Array.ofSeq)
        cmd
    member _.ExecuteSqlReaderAsync (sql : string, queryParams : NpgsqlParameter IReadOnlyCollection) =
        task {
            use cmd = new NpgsqlCommand (sql, tx.Connection)
            cmd.Transaction <- tx
            cmd.Parameters.AddRange (queryParams |> Array.ofSeq)
            return! cmd.ExecuteReaderAsync ()
        }
    member _.ExecuteSqlNonQueryAsync (sql : string, queryParams : NpgsqlParameter IReadOnlyCollection) =
        task {
            use cmd = new NpgsqlCommand (sql, tx.Connection)
            cmd.Transaction <- tx
            cmd.Parameters.AddRange (queryParams |> Array.ofSeq)
            return! cmd.ExecuteNonQueryAsync ()
        }
    member _.ExecuteSqlScalarAsync (sql : string, queryParams : NpgsqlParameter IReadOnlyCollection) =
        task {
            use cmd = new NpgsqlCommand (sql, tx.Connection)
            cmd.Transaction <- tx
            cmd.Parameters.AddRange (queryParams |> Array.ofSeq)
            return! cmd.ExecuteScalarAsync ()
        }
    member _.CommitAsync () = task { return! tx.CommitAsync () }
    member _.RollbackAsync () = task { return! tx.RollbackAsync () }
    interface IDbConnection with
        member this.ExecuteSqlNonQuery sql queryParams = task { return! this.ExecuteSqlNonQueryAsync(sql, queryParams) }
        member this.ExecuteSqlReader sql queryParams = task { return! this.ExecuteSqlReaderAsync(sql, queryParams) }
        member this.ExecuteSqlScalar sql queryParams = task { return! this.ExecuteSqlScalarAsync(sql, queryParams) }

    interface IDisposable with
        member this.Dispose () = tx.Dispose ()

    interface IAsyncDisposable with
        member _.DisposeAsync () = tx.DisposeAsync ()

module private DbConnection_impl =
    let private toQueryParams (queryParams : (string * obj) seq) =
        queryParams
        |> Seq.map NpgsqlParameter
        |> Array.ofSeq

    let private createCommandWithParamsAsync (db : NpgsqlConnection) (sql : string) (queryParams : NpgsqlParameter seq) =
        let cmd = new NpgsqlCommand (sql, db)
        cmd.Parameters.AddRange (queryParams |> Array.ofSeq)
        task {
            let! _ = cmd.PrepareAsync ()
            return cmd
        }

    let private createCommandAsync (db : NpgsqlConnection) (sql : string) (queryParams : (string * obj) seq) =
        queryParams
        |> Seq.map NpgsqlParameter
        |> createCommandWithParamsAsync db sql

    let connectToDbIfNotConnectedAsync (db : NpgsqlConnection) =
        match db.State with
        | ConnectionState.Closed ->
            task { return! db.OpenAsync () }
        | _ -> task { return! Task.CompletedTask }

    let executeSqlNonQueryAsync (db : NpgsqlConnection) (sql : string) (queryParams : NpgsqlParameter seq) =
        task {
            let! _ = db |> connectToDbIfNotConnectedAsync
            use! cmd = (sql, queryParams) ||> createCommandWithParamsAsync db
            return! cmd.ExecuteNonQueryAsync ()
        }

    let executeScalarAsync (db : NpgsqlConnection) (sql : string) (queryParams : NpgsqlParameter seq) =
        task {
            let! _ = db |> connectToDbIfNotConnectedAsync
            use! cmd = (sql, queryParams) ||> createCommandWithParamsAsync db
            return! cmd.ExecuteScalarAsync ()
        }

    let executeReaderAsync (db : NpgsqlConnection) (sql : string) (queryParams : NpgsqlParameter seq) =
        task {
            let! _ = db |> connectToDbIfNotConnectedAsync
            use! cmd = (sql, queryParams) ||> createCommandWithParamsAsync db
            return! cmd.ExecuteReaderAsync ()
        }

    let createTransactionAsync (db : NpgsqlConnection) =
        task {
            let! _ = db |> connectToDbIfNotConnectedAsync
            let! tx = db.BeginTransactionAsync ()
            return new DbTransactionWrapper (tx)
        }

type DbConnection (settings : DbSettings) =
    let connectionString =
        let builder = NpgsqlConnectionStringBuilder ()
        builder.Host <- settings.HostName
        builder.Database <- settings.DatabaseName
        builder.Username <- settings.Username
        builder.Password <- settings.Password
        builder.ToString ()

    let db = new NpgsqlConnection(connectionString)
    member internal _.__RawDb = db

    member _.ExecuteSqlNonQueryAsync (sql : string, [<ParamArray>] queryParams : NpgsqlParameter IReadOnlyCollection) =
        (sql, queryParams)
        ||> DbConnection_impl.executeSqlNonQueryAsync db

    member _.ExecuteScalarAsync (sql : string, [<ParamArray>] queryParams : NpgsqlParameter IReadOnlyCollection) =
        (sql, queryParams)
        ||> DbConnection_impl.executeScalarAsync db

    member _.ExecuteReaderAsync (sql : string, [<ParamArray>] queryParams : NpgsqlParameter IReadOnlyCollection) : DbDataReader Task =
        (sql, queryParams)
        ||> DbConnection_impl.executeReaderAsync db

    member _.CreateTransactionAsync () : DbTransactionWrapper Task =
        DbConnection_impl.createTransactionAsync db

    interface IDbConnection with
        member this.ExecuteSqlNonQuery sql queryParams = task { return! this.ExecuteSqlNonQueryAsync(sql, queryParams) }
        member this.ExecuteSqlReader sql queryParams = task { return! this.ExecuteReaderAsync(sql, queryParams) }
        member this.ExecuteSqlScalar sql queryParams = task { return! this.ExecuteScalarAsync(sql, queryParams) }

    interface IDisposable with
        member _.Dispose () =
            db.Dispose ()

type DbTransactionFactory (db : DbConnection) =
    member _.CreateTransaction () : DbTransactionWrapper Task = DbConnection_impl.createTransactionAsync db.__RawDb