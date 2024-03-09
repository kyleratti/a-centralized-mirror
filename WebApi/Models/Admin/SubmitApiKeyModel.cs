using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Admin;

/// <summary>
/// Model to generate a new API key.
/// </summary>
public record SubmitApiKeyModel
{
	/// <summary>
	/// The id of the user to generate the API key for.
	/// </summary>
	[Required] public int? UserId { get; init; }
}
