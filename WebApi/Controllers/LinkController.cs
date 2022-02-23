using System.Net;
using ApplicationData.Locale;
using ApplicationData.Services;
using DataClasses;
using FruityFoundation.Base.Extensions;
using Microsoft.AspNetCore.Mvc;
using SnooBrowser.Browsers;
using SnooBrowser.Things;
using WebApi.Models;
using WebApi.Util;

namespace WebApi.Controllers;

/// <summary>
/// LinkController
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class LinkController : ApiController
{
	private readonly LinkProvider _linkProvider;
	private readonly SubmissionBrowser _submissionBrowser;

	/// <summary>
	/// C'tor
	/// </summary>
	public LinkController(
		IServiceProvider serviceProvider,
		LinkProvider linkProvider,
		SubmissionBrowser submissionBrowser
	) : base(serviceProvider)
	{
		_linkProvider = linkProvider;
		_submissionBrowser = submissionBrowser;
	}

	/// <summary>
	/// Submit a new link for the specified reddit post.
	/// </summary>
	[HttpPost]
	[Route("")]
	[ProducesResponseType((int)HttpStatusCode.Created)]
	public async Task<IActionResult> SubmitLink([FromBody] SubmitLinkRequest linkRequest)
	{
		var validUrl = GetValidUriOrFail(linkRequest.LinkUrl);
		var linkKind = GetLinkKindOrFail(linkRequest.LinkType!.Value);

		if ((await _linkProvider.FindLink(UserId, linkRequest.RedditPostId, validUrl.OriginalString, linkKind)).HasValue)
			return new ConflictObjectResult(new
			{
				message = TranslatedStrings.LinkController.LinkAlreadyExists,
				redditPostId = linkRequest.RedditPostId,
				url = validUrl.OriginalString,
				linkType = linkRequest.LinkType
			});

		var maybeSubmission = await _submissionBrowser.GetSubmission(LinkThing.CreateFromShortId(linkRequest.RedditPostId));

		if (!maybeSubmission.Try(out var submission) || submission.IsArchived || submission.IsLocked)
			return new BadRequestObjectResult(new
			{
				message = TranslatedStrings.LinkController.RedditPostIdIsNotValid(linkRequest.RedditPostId),
				redditPostId = linkRequest.RedditPostId
			});

		await _linkProvider.CreateLink(new NewLink(
			redditPostId: linkRequest.RedditPostId,
			linkUrl: validUrl.OriginalString,
			linkKind,
			ownerUserId: UserId
		));

		// We are technically violating the HTTP spec here but not sending back the location of the resource we just created, but oh well.
		return new StatusCodeResult((int)HttpStatusCode.Created);
	}

	/// <summary>
	/// Delete an existing link for the specified reddit post.
	/// </summary>
	[HttpDelete]
	[Route("")]
	[ProducesResponseType((int)HttpStatusCode.OK)]
	public async Task<IActionResult> DeleteLinkByLinkData([FromBody] DeleteLinkRequest linkRequest)
	{
		var validUrl = GetValidUriOrFail(linkRequest.LinkUrl);
		var linkKind = GetLinkKindOrFail(linkRequest.LinkType!.Value);

		if (!(await _linkProvider.FindLink(UserId, linkRequest.RedditPostId, validUrl.OriginalString, linkKind)).Try(out var link))
			return new NotFoundObjectResult(new
			{
				message = TranslatedStrings.LinkController.LinkNotFound,
				redditPostId = linkRequest.RedditPostId,
				url = validUrl.OriginalString
			});

		await _linkProvider.DeleteLinkById(link.LinkId);

		return new OkObjectResult(new
		{
			message = TranslatedStrings.LinkController.LinkDeleted,
			redditPostId = link.RedditPostId
		});
	}

	/// <summary>
	/// Delete an existing link by using the provided link ID.
	/// </summary>
	[HttpDelete]
	[Route("{linkId:int}")]
	[ProducesResponseType((int)HttpStatusCode.OK)]
	public async Task<IActionResult> DeleteLinkById([FromRoute] int linkId)
	{
		if (!(await _linkProvider.FindLinkById(UserId, linkId)).Try(out var link))
			return new NotFoundObjectResult(new
			{
				message = TranslatedStrings.LinkController.LinkIdNotFound(linkId),
				linkId
			});

		await _linkProvider.DeleteLinkById(link.LinkId);
		
		return new OkObjectResult(new
		{
			message = TranslatedStrings.LinkController.LinkDeleted,
			redditPostId = link.RedditPostId
		});
	}

	/// <summary>
	/// Get all links currently managed for this user.
	/// </summary>
	[HttpGet]
	[Route("All")]
	public async Task<IReadOnlyCollection<SerializableLink>> GetAllLinksAsync() =>
		(await _linkProvider.GetAllLinksByUserId(UserId))
		.Select(x => new SerializableLink(
			x.LinkId,
			x.RedditPostId,
			x.LinkUrl,
			LinkTypeHelpers.ParseToSerializableLinkType(x.LinkType.RawValue),
			x.CreatedAt
		))
		.ToArray();

	private static Uri GetValidUriOrFail(string linkUrl)
	{
		if (!Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri))
			throw new ApplicationException($"Unable to parse provided URL: {linkUrl}");

		if (!uri.Scheme.EqualsIgnoreCase("https"))
			throw new ArgumentOutOfRangeException("scheme", uri.Scheme,
				"You must provide a link using the HTTPS scheme")
			{
				Data = { { "HttpStatusCode", HttpStatusCode.BadRequest } }
			};

		return uri;
		}

	private static LinkKind GetLinkKindOrFail(SerializableLinkType serializableLinkType) => serializableLinkType switch
	{
		SerializableLinkType.Mirror => LinkKind.Mirror,
		SerializableLinkType.Download => LinkKind.Download,
		_ => throw new ArgumentOutOfRangeException(nameof(serializableLinkType), serializableLinkType, "Unknown link type")
	};
}