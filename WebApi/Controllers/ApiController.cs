using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591

namespace WebApi.Controllers;

[ApiController]
public class ApiController(
	IHttpContextAccessor _httpContext
)
{
	protected int UserId =>
		Convert.ToInt32(_httpContext.HttpContext!.User.Claims.First(x => x.Type.Equals("UserId", StringComparison.OrdinalIgnoreCase)).Value);
}
