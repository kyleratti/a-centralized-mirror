using ApplicationData.Services;
using DataClasses;
using FruityFoundation.Base.Structures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages;

/// <summary>
/// Search result
/// </summary>
public record SearchResult(Maybe<string> PostTitle, IReadOnlyCollection<(Link Link, User Owner)> Links);

/// <summary>
/// Index Model
/// </summary>
public class IndexModel : PageModel
{
	private readonly LinkProvider _linkProvider;
	private readonly UserProvider _userProvider;
	private readonly RedditPostProvider _redditPostProvider;

	/// <summary>
	/// C'tor
	/// </summary>
	public IndexModel(LinkProvider linkProvider, UserProvider userProvider, RedditPostProvider redditPostProvider)
	{
		_linkProvider = linkProvider;
		_userProvider = userProvider;
		_redditPostProvider = redditPostProvider;
	}

	/// <summary>
	/// Search results
	/// </summary>
	public Maybe<SearchResult> SearchResults = Maybe.Empty<SearchResult>();

	/// <summary>
	/// Reddit Post Id
	/// </summary>
	[BindProperty(SupportsGet = true)]
	public string? RedditPostId { get; set; }

	/// <summary>
	/// GET /
	/// </summary>
	public async Task OnGet(CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(RedditPostId))
			return;

		var linksWithOwners = await _linkProvider.FindAllLinksByPostId(RedditPostId, cancellationToken)
			.SelectAwait(async link =>
			{
				if (!(await _userProvider.FindUserByIdIncludeDeleted(link.OwnerId)).Try(out var owner))
					throw new ApplicationException($"Link {link.LinkId} has an owner ID {link.OwnerId} that does not exist.");

				return (Link: link, Owner: owner);
			})
			.ToArrayAsync(cancellationToken);

		var postTitle = await _redditPostProvider.GetPostTitleByPostId(RedditPostId, cancellationToken);

		SearchResults = new SearchResult(postTitle, linksWithOwners);
	}
}
