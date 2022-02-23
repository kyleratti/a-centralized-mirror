namespace WebApi.Models;

public record SerializableLink(
	int LinkId,
	string RedditPostId,
	string LinkUrl,
	SerializableLinkType LinkType,
	DateTime CreatedAt
);