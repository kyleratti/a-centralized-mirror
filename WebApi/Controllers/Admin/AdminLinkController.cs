using ApplicationData.Services;
using FruityFoundation.Base.Structures;
using Microsoft.AspNetCore.Mvc;
#pragma warning disable CS1591

namespace WebApi.Controllers.Admin;

[Route("v1/admin/link")]
public class AdminLinkController : AdminApiController
{
	private readonly LinkProvider _linkProvider;

	public AdminLinkController(
		IServiceProvider serviceProvider,
		LinkProvider linkProvider
	) : base(serviceProvider)
	{
		_linkProvider = linkProvider;
	}

	[HttpDelete]
	[Route("{linkId:int}")]
	public async Task<IActionResult> DeleteLinkAsync(int linkId)
	{
		if (!(await _linkProvider.FindLinkById(userId: Maybe<int>.Empty(), linkId)).HasValue)
			return new NotFoundResult();

		await _linkProvider.DeleteLinkById(linkId);

		return new OkResult();
	}
}