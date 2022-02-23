namespace Core.AppSettings

open Microsoft.Extensions.Options

[<CLIMutable>]
type RawDbSettings = {
    HostName : string
    DatabaseName : string
    Username : string
    Password : string
}

type DbSettings (config : IOptions<RawDbSettings>) =
    member _.HostName = config.Value.HostName
    member _.DatabaseName = config.Value.DatabaseName
    member _.Username = config.Value.Username
    member _.Password = config.Value.Password

[<CLIMutable>]
type RawRedditSettings = {
    AppId : string
    AppSecret : string
    RefreshToken : string
}

type RedditSettings (config : IOptions<RawRedditSettings>) =
    member _.AppId = config.Value.AppId
    member _.AppSecret = config.Value.AppSecret
    member _.RefreshToken = config.Value.RefreshToken