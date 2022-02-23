namespace WebApi.Models;

/// <summary>
/// Information about the user you're currently authorized as.
/// </summary>
/// <param name="DisplayName">The name displayed in reddit comments as the provider of the link.</param>
/// <param name="DeveloperName">The name of reddit user responsible for this bot. This is **NOT** displayed publicly anywhere and is only used for when the developer needs to be contacted about this service.</param>
/// <param name="CreatedAt">The timestamp of when the user was created.</param>
public record AboutMe(
	string DisplayName,
	string DeveloperName,
	DateTime CreatedAt
);