using ApplicationData.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class MeController : ApiController
{
	private readonly LinkProvider _linkProvider;

	public MeController(
		IServiceProvider serviceProvider,
		LinkProvider linkProvider
	) : base(serviceProvider)
	{
		_linkProvider = linkProvider;
	}

	/// <summary>
	/// Retrieve information about the current API user.
	/// </summary>
	/// <returns></returns>
	[HttpGet]
	[Route("")]
	public AboutMe GetAboutMe() =>
		new(
			User.DisplayUsername,
			User.DeveloperUsername,
			User.CreatedAt
		);
}