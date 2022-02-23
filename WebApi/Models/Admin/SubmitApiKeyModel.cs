using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Admin;

public record SubmitApiKeyModel
{
	[Required] public int? UserId { get; init; }
}