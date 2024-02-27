namespace Core.AppSettings

[<CLIMutable>]
type RedditSettings = {
    AppId : string
    AppSecret : string
    RefreshToken : string
}
