using ApplicationData.Locale;
using ApplicationData.Services;
using DataClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SnooBrowser.Browsers;
using SnooBrowser.Things;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;
using WebApi.Util;

namespace WebApi.Controllers;

/// <summary>
/// LinkController
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class LinkController : Controller
{
	private readonly LinkProvider _linkProvider;
	private readonly SubmissionBrowser _submissionBrowser;
	private readonly ResourceAccessManager _resourceAccessManager;

	/// <summary>
	/// C'tor
	/// </summary>
	public LinkController(
		LinkProvider linkProvider,
		SubmissionBrowser submissionBrowser,
		ResourceAccessManager resourceAccessManager
	)
	{
		_linkProvider = linkProvider;
		_submissionBrowser = submissionBrowser;
		_resourceAccessManager = resourceAccessManager;
	}

	/// <summary>
	/// Submit a new link for the specified reddit post.
	/// </summary>
	[HttpPost]
	[Route("")]
	[SwaggerResponse(StatusCodes.Status201Created)]
	[SwaggerResponse(StatusCodes.Status400BadRequest,
		description: "Bad Request. The request could not be processed due to invalid data on the request.",
		typeof(BadRequestError))]
	[SwaggerResponse(StatusCodes.Status409Conflict,
		description: "Conflict. This user has already provided this combination of reddit post ID, URL, and link type.",
		typeof(LinkAlreadyExistsError))]
	public async Task<IActionResult> SubmitLink([FromBody] SubmitLinkRequest linkRequest, CancellationToken cancellationToken)
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("User is not logged in");

		var validUrl = GetValidUriOrFail(linkRequest.LinkUrl);
		var linkKind = GetLinkKindOrFail(linkRequest.LinkType!.Value);

		var maybeSubmission = await _submissionBrowser.GetSubmission(LinkThing.CreateFromShortId(linkRequest.RedditPostId!));

		if (!maybeSubmission.Try(out var submission) || submission.IsArchived || submission.IsLocked)
			return new BadRequestObjectResult(new BadRequestError(
				Message: TranslatedStrings.LinkController.RedditPostIdIsNotValid(linkRequest.RedditPostId!),
				RedditPostId: linkRequest.RedditPostId!
			));

		var createResult = await _resourceAccessManager.WithExclusiveAccess(
			linkRequest.RedditPostId!,
			async () =>
				await _linkProvider.CreateLink(new NewLink(
					redditPostId: linkRequest.RedditPostId,
					redditPostTitle: submission.Title,
					linkUrl: validUrl.OriginalString,
					linkKind,
					ownerUserId: loggedInUserId
				)),
			cancellationToken);

		if (createResult.IsFailure)
		{
			return createResult.FailureVal.Switch(
				linkAlreadyExists: () => new ConflictObjectResult(new LinkAlreadyExistsError(
					Message: TranslatedStrings.LinkController.LinkAlreadyExists,
					RedditPostId: linkRequest.RedditPostId!,
					Url: validUrl.OriginalString,
					LinkType: linkRequest.LinkType
				)));
		}

		// We are technically violating the HTTP spec here but not sending back the location of the resource we just created, but oh well.
		return new StatusCodeResult(StatusCodes.Status201Created);
	}

	/// <summary>
	/// Delete an existing link for the specified reddit post.
	/// </summary>
	[HttpDelete]
	[Route("")]
	[SwaggerResponse(StatusCodes.Status200OK,
		description: "OK. The link has been queued for deletion.",
		typeof(LinkDeleteQueuedSuccessfully))]
	[SwaggerResponse(StatusCodes.Status404NotFound,
		description: "Not Found. This user does not have this combination of reddit post ID, URL, and link type.",
		typeof(LinkNotFoundError))]
	public async Task<IActionResult> DeleteLinkByLinkData([FromBody] DeleteLinkRequest linkRequest, CancellationToken cancellationToken)
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("User is not logged in");

		var validUrl = GetValidUriOrFail(linkRequest.LinkUrl);
		var linkKind = GetLinkKindOrFail(linkRequest.LinkType!.Value);

		if (!(await _linkProvider.FindLink(loggedInUserId, linkRequest.RedditPostId, validUrl.OriginalString, linkKind)).Try(out var link))
			return new NotFoundObjectResult(new LinkNotFoundError(
				Message: TranslatedStrings.LinkController.LinkNotFound,
				RedditPostId: linkRequest.RedditPostId,
				Url: validUrl.OriginalString
			));

		await _resourceAccessManager.WithExclusiveAccess(
			linkRequest.RedditPostId,
			() => _linkProvider.DeleteLinkById(link.LinkId),
			cancellationToken);

		return new OkObjectResult(new LinkDeleteQueuedSuccessfully(
			Message: TranslatedStrings.LinkController.LinkDeleted,
			RedditPostId: link.RedditPostId,
			Url: link.LinkUrl
		));
	}

	/// <summary>
	/// Delete an existing link by using the provided link ID.
	/// </summary>
	[HttpDelete]
	[Route("{linkId:int}")]
	[SwaggerResponse(StatusCodes.Status200OK,
		description: "OK. The link has been queued for deletion.",
		typeof(LinkDeleteQueuedSuccessfully))]
	[SwaggerResponse(StatusCodes.Status404NotFound,
		description: "Not Found. This user does not have this combination of reddit post ID, URL, and link type.",
		typeof(LinkIdNotFoundError))]
	public async Task<IActionResult> DeleteLinkById([FromRoute] int linkId, CancellationToken cancellationToken)
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("User is not logged in");

		if (!(await _linkProvider.FindLinkById(loggedInUserId, linkId)).Try(out var link))
			return new NotFoundObjectResult(new LinkIdNotFoundError(
				Message: TranslatedStrings.LinkController.LinkIdNotFound(linkId),
				LinkId: linkId
			));

		await _resourceAccessManager.WithExclusiveAccess(
			link.RedditPostId,
			() => _linkProvider.DeleteLinkById(link.LinkId),
			cancellationToken);
		
		return new OkObjectResult(new LinkDeleteQueuedSuccessfully(
			Message: TranslatedStrings.LinkController.LinkDeleted,
			RedditPostId: link.RedditPostId,
			Url: link.LinkUrl
		));
	}

	/// <summary>
	/// Get all links currently managed for this user.
	/// </summary>
	[HttpGet]
	[Route("All")]
	public async Task<IReadOnlyCollection<SerializableLink>> GetAllLinksAsync()
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("User is not logged in");

		return (await _linkProvider.GetAllLinksByUserId(loggedInUserId))
			.Select(x => new SerializableLink(
				x.LinkId,
				x.RedditPostId,
				x.LinkUrl,
				LinkTypeHelpers.ParseToSerializableLinkType(x.LinkType.RawValue),
				x.CreatedAt
			))
			.ToArray();
	}

	private static Uri GetValidUriOrFail(string? linkUrl)
	{
		if (!Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri))
			throw new ApplicationException($"Unable to parse provided URL: {linkUrl}");

		if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentOutOfRangeException("scheme", uri.Scheme,
				"You must provide a link using the HTTPS scheme")
			{
				Data = { { "HttpStatusCode", StatusCodes.Status400BadRequest } }
			};

		return uri;
	}

	private static LinkKind GetLinkKindOrFail(SerializableLinkType serializableLinkType) => serializableLinkType switch
	{
		SerializableLinkType.Mirror => LinkKind.Mirror,
		SerializableLinkType.Download => LinkKind.Download,
		_ => throw new ArgumentOutOfRangeException(nameof(serializableLinkType), serializableLinkType, "Unknown link type")
	};

	/// <summary>
	/// The error message returned when the submitted request has invalid data that must be corrected before it can be processed.
	/// </summary>
	/// <param name="Message">The error message.</param>
	/// <param name="RedditPostId">The reddit post ID.</param>
	private record BadRequestError(
		[property:JsonProperty("message")] string Message,
		[property:JsonProperty("redditPostId")] string RedditPostId);

	/// <summary>
	/// The data model returned when a link is successfully queued for deletion.
	/// </summary>
	/// <param name="Message">The message from the server.</param>
	/// <param name="RedditPostId">The reddit post ID.</param>
	/// <param name="Url">The link that will be deleted from the list of links on the post.</param>
	private record LinkDeleteQueuedSuccessfully(
		[property:JsonProperty("message")] string Message,
		[property:JsonProperty("redditPostId")] string RedditPostId,
		[property:JsonProperty("url")] string Url);

	/// <summary>
	/// The data model returned when a link specific link on a reddit post is not found but the API consumer expected it to be.
	/// </summary>
	/// <param name="Message">The message from the server.</param>
	/// <param name="RedditPostId">The reddit post ID.</param>
	/// <param name="Url">The provided URL.</param>
	private record LinkNotFoundError(
		[property:JsonProperty("message")] string Message,
		[property:JsonProperty("redditPostId")] string RedditPostId,
		[property:JsonProperty("url")] string Url);

	/// <summary>
	/// The data model returned when the API consumer provided a specific link ID to delete but that link ID did not exist.
	/// </summary>
	/// <param name="Message">The message from the server.</param>
	/// <param name="LinkId">The provided link ID.</param>
	private record LinkIdNotFoundError(
		[property:JsonProperty("message")] string Message,
		[property:JsonProperty("linkId")] int LinkId);

	/// <summary>
	/// The error message returned when the provided link already exists for the specified reddit post ID.
	/// </summary>
	/// <param name="Message">The error message from the API.</param>
	/// <param name="RedditPostId">The reddit post ID.</param>
	/// <param name="Url">The URL that was submitted.</param>
	/// <param name="LinkType">The type of link that was submitted.</param>
	private record LinkAlreadyExistsError(
		[property: JsonProperty("message")] string Message,
		[property: JsonProperty("redditPostId")] string RedditPostId,
		[property: JsonProperty("url")] string Url,
		[property: JsonProperty("linkType")] SerializableLinkType? LinkType);
}
