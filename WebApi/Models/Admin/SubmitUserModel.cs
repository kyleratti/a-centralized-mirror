using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Admin;

/// <summary>
/// The model to submit a new user.
/// </summary>
public record SubmitUserModel
{
	/// <summary>
	/// The display username to use in reddit posts.
	/// Do NOT include the /u/ prefix.
	/// </summary>
	[Required] public string? DisplayUsername { get; init; }

	/// <summary>
	/// The developer username on reddit.
	/// This is NOT displayed anywhere and is only for administrative reference.
	/// Do NOT include the /u/ prefix.
	/// </summary>
	[Required] public string? DeveloperUsername { get; init; }

	/// <summary>
	/// The weight of this user when building comments.
	/// </summary>
	[Required] public int? Weight { get; init; }

	/// <summary>
	/// Is the user an administrator?
	/// </summary>
	[Required] public bool? IsAdministrator { get; init; }
}
