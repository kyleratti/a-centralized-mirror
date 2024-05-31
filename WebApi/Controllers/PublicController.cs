using ApplicationData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;
using WebApi.Util;

namespace WebApi.Controllers;

/// <summary>
/// PublicController
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[AllowAnonymous]
public class PublicController : Controller
{
	private readonly LinkProvider _linkProvider;
	private readonly UserProvider _userProvider;

	/// <summary>
	/// C'tor
	/// </summary>
	public PublicController(
		LinkProvider linkProvider,
		UserProvider userProvider
	)
	{
		_linkProvider = linkProvider;
		_userProvider = userProvider;
	}

	/// <summary>
	/// Find links available for the specified reddit post ID.
	/// </summary>
	/// <param name="redditPostId">The unique reddit post ID. For example, use "sf8kp8" from the permalink "https://www.reddit.com/r/aww/comments/sf8kp8/treats_you_say/".</param>
	[HttpGet]
	[Route("Link/By-Reddit-Post-Id/{redditPostId}")]
	[SwaggerResponse(StatusCodes.Status200OK,
		description: "OK. A list of links available for this reddit post ID.",
		type: typeof(IReadOnlyCollection<PublicLinkListingResult>))]
	public async Task<IReadOnlyCollection<PublicLinkListingResult>> FindLinksByPostId(string redditPostId)
	{
		var linksByPostId = await _linkProvider.FindAllLinksByPostId(redditPostId);
		return await linksByPostId.ToAsyncEnumerable()
			.SelectAwait(async link => new PublicLinkListingResult(
				link.LinkUrl,
				LinkTypeHelpers.ParseToSerializableLinkType(link.LinkType.RawValue),
				ProviderUsername: (await _userProvider.FindUserByIdIncludeDeleted(link.OwnerId)).Map(x => x.DisplayUsername).Value))
			.ToArrayAsync();
	}

	/// <summary>
	/// A link listing result accessible to the public.
	/// </summary>
	public record PublicLinkListingResult(
		[JsonProperty("linkUrl")] string LinkUrl,
		[JsonProperty("linkType")] SerializableLinkType LinkType,
		[JsonProperty("providerUsername")] string ProviderUsername
	);
}
