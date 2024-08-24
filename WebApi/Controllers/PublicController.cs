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
	private readonly RedditPostProvider _redditPostProvider;

	/// <summary>
	/// C'tor
	/// </summary>
	public PublicController(
		LinkProvider linkProvider,
		UserProvider userProvider,
		RedditPostProvider redditPostProvider
	)
	{
		_linkProvider = linkProvider;
		_userProvider = userProvider;
		_redditPostProvider = redditPostProvider;
	}

	/// <summary>
	/// Find links available for the specified reddit post ID.
	/// </summary>
	/// <param name="redditPostId">The unique reddit post ID. For example, use "sf8kp8" from the permalink "https://www.reddit.com/r/aww/comments/sf8kp8/treats_you_say/".</param>
	/// <param name="cancellationToken"></param>
	[HttpGet]
	[Route("Link/By-Reddit-Post-Id/{redditPostId}")]
	[SwaggerResponse(StatusCodes.Status200OK,
		description: "OK. A list of links available for this reddit post ID.",
		type: typeof(IReadOnlyCollection<PublicLinkListingResult>))]
	public async Task<IReadOnlyCollection<PublicLinkListingResult>> FindLinksByPostId(string redditPostId, CancellationToken cancellationToken)
	{
		return await _linkProvider.FindAllLinksByPostId(redditPostId, cancellationToken)
			.SelectAwait(async link =>
			{
				var postTitle = await _redditPostProvider.GetPostTitleByPostId(link.RedditPostId, cancellationToken);
				var providerUsername = (await _userProvider.FindUserByIdIncludeDeleted(link.OwnerId))
					.Map(x => x.DisplayUsername)
					.OrThrow(() => $"Unable to find user info for user id {link.OwnerId}");

				return new PublicLinkListingResult(
					RedditPostId: link.RedditPostId,
					PostTitle: postTitle.Cast<string?>().OrValue(null),
					link.LinkUrl,
					LinkTypeHelpers.ParseToSerializableLinkType(link.LinkType.RawValue),
					ProviderUsername: providerUsername);
			})
			.ToArrayAsync(cancellationToken: cancellationToken);
	}

	/// <summary>
	/// A link listing result accessible to the public.
	/// </summary>
	/// <param name="RedditPostId">The reddit post id (without prefix, e.g., "abc123").</param>
	/// <param name="PostTitle">The title of the reddit post. This may be null if the title was not stored when the link was created.</param>
	/// <param name="LinkUrl">The URL of the link.</param>
	/// <param name="LinkType">The type of the link.</param>
	/// <param name="ProviderUsername">The username of the provider of the link (without prefix, e.g., "a-mirror-bot").</param>
	public record PublicLinkListingResult(
		[JsonProperty("redditPostId")] string RedditPostId,
		[JsonProperty("postTitle")] string? PostTitle,
		[JsonProperty("linkUrl")] string LinkUrl,
		[JsonProperty("linkType")] SerializableLinkType LinkType,
		[JsonProperty("providerUsername")] string ProviderUsername
	);
}
