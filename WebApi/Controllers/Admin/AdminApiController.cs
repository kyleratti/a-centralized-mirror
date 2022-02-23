using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Util;
#pragma warning disable CS1591

namespace WebApi.Controllers.Admin;

[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminApiController : ApiController
{
	public AdminApiController(
		IServiceProvider serviceProvider
	) : base(serviceProvider)
	{
		//
	}
}