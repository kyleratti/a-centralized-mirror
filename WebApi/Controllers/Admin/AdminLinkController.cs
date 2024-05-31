using ApplicationData.Services;
using FruityFoundation.Base.Structures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Util;

#pragma warning disable CS1591

namespace WebApi.Controllers.Admin;

[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/link")]
public class AdminLinkController : Controller
{
	private readonly LinkProvider _linkProvider;

	public AdminLinkController(
		LinkProvider linkProvider
	)
	{
		_linkProvider = linkProvider;
	}

	[HttpDelete]
	[Route("{linkId:int}")]
	public async Task<IActionResult> DeleteLinkAsync(int linkId)
	{
		if (!(await _linkProvider.FindLinkById(userId: Maybe.Empty<int>(), linkId)).HasValue)
			return new NotFoundResult();

		await _linkProvider.DeleteLinkById(linkId);

		return new OkResult();
	}
}
