namespace WebApi.Models;

/// <summary>
/// A mirror link.
/// </summary>
/// <param name="LinkId">The id of the link.</param>
/// <param name="RedditPostId">The reddit post id slug (without any prefixes).</param>
/// <param name="LinkUrl">The url of the link.</param>
/// <param name="LinkType">The type of link.</param>
/// <param name="CreatedAt">When the link was created.</param>
public record SerializableLink(
	int LinkId,
	string RedditPostId,
	string LinkUrl,
	SerializableLinkType LinkType,
	DateTime CreatedAt
);
