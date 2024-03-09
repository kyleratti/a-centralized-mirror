using System.ComponentModel.DataAnnotations;

namespace WebApi.Models;

/// <summary>
/// A request to submit a new link.
/// </summary>
public record SubmitLinkRequest
{
	/// <summary>
	/// The unique reddit post ID. For example, use "sf8kp8" from the permalink "https://www.reddit.com/r/aww/comments/sf8kp8/treats_you_say/".
	/// </summary>
	[Required] public string RedditPostId { get; init; }

	/// <summary>
	/// The full URL to the external service for this link. For example, "https://mymirrorsite.com/sf8kp8.html".
	/// </summary>
	[Required] public string LinkUrl { get; init; }

	/// <summary>
	/// The kind of link this is. Either "mirror" or "download".
	/// </summary>
	[Required] public SerializableLinkType? LinkType { get; init; }
}
