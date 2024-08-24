namespace DataClasses

open System

type User = {
    UserId : int32
    DisplayUsername : string
    DeveloperUsername : string
    Weight : int32
    CreatedAt : DateTime
    UpdatedAt : DateTime option
    IsDeleted : bool
    IsAdministrator : bool
}

type RawLinkKind =
    | Mirror = 1s
    | Download = 2s

type LinkKind =
    | Mirror
    | Download
    member this.RawValue =
        match this with
        | Mirror -> 1s
        | Download -> 2s

type Link = {
    LinkId : int32
    RedditPostId : string
    LinkUrl : string
    LinkType : LinkKind
    CreatedAt : DateTime
    OwnerId : int32
}

type NewLink = {
    RedditPostId : string
    RedditPostTitle : string
    LinkUrl : string
    LinkType : LinkKind
    OwnerUserId : int32
}

type NewUser = {
    DisplayUsername : string
    DeveloperUsername : string
    Weight : int32 option
    IsAdministrator : bool
}
