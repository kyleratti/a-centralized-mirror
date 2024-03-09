namespace WebApi.Models.Admin;

/// <summary>
/// The model of a user for administrative purposes.
/// </summary>
/// <param name="UserId">The id of the user.</param>
/// <param name="DisplayUsername">The reddit username to use for display, without the /u/ prefix.</param>
/// <param name="DeveloperUsername">The reddit username of the bot author. For admin reference only. No /u/ prefix.</param>
/// <param name="Weight">The weight of this user when generating comments.</param>
/// <param name="CreatedAt">The timestamp of when the user was created.</param>
/// <param name="UpdatedAt">The timestamp of when the user was last updated (if ever).</param>
/// <param name="IsDeleted">Is the user deleted?</param>
/// <param name="IsAdministrator">Is the user an administrator?</param>
public record UserAdminModel(
	int UserId,
	string DisplayUsername,
	string DeveloperUsername,
	int Weight,
	DateTime CreatedAt,
	DateTime? UpdatedAt,
	bool IsDeleted,
	bool IsAdministrator
);
