using System.ComponentModel.DataAnnotations;
// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Models;

/// <summary>
/// 
/// </summary>
/// <param name="RedditPostId">The unique reddit post ID. For example, use "sf8kp8" from the permalink "https://www.reddit.com/r/aww/comments/sf8kp8/treats_you_say/".</param>
/// <param name="LinkUrl">The full URL to the external service for this link. For example, "https://mymirrorsite.com/sf8kp8.html".</param>
/// <param name="LinkType">The kind of link this is. Either "mirror" or "download".</param>
public record DeleteLinkRequest(
	[Required] string RedditPostId,
	[Required] string LinkUrl,
	[Required] SerializableLinkType? LinkType
);