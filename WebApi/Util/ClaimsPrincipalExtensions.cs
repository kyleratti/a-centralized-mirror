using System.Security.Claims;
using FruityFoundation.Base.Structures;

namespace WebApi.Util;

/// <summary>
/// ClaimsPrincipal extensions
/// </summary>
public static class ClaimsPrincipalExtensions
{
	/// <summary>
	/// Try to get the user id from the claims principal.
	/// </summary>
	public static Maybe<int> GetUserId(this ClaimsPrincipal claimsPrincipal)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null)
			return Maybe.Empty<int>();

		if (!int.TryParse(userIdClaim.Value, out var userId))
			return Maybe.Empty<int>();

		return userId;
	}
}
