using ApplicationData.Services;
using DataClasses;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591

namespace WebApi.Controllers;

[ApiController]
public class ApiController
{
	private readonly UserCache _userCache;
	private readonly IHttpContextAccessor _httpContext;

	public ApiController(
		IServiceProvider serviceProvider
	)
	{
		_userCache = serviceProvider.GetRequiredService<UserCache>();
		_httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>();
	}

	public int UserId =>
		Convert.ToInt32(_httpContext.HttpContext!.User.Claims.First(x => x.Type.Equals("UserId", StringComparison.OrdinalIgnoreCase)).Value);

	public User User => _userCache.GetUserOrFail(UserId);
}
