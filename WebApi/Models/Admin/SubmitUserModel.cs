using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Admin;

public record SubmitUserModel
{
	[Required] public string? DisplayUsername { get; init; }
	[Required] public string? DeveloperUsername { get; init; }
	[Required] public int? Weight { get; init; }
	[Required] public bool? IsAdministrator { get; init; }
}