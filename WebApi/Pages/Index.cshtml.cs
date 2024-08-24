using ApplicationData.Services;
using DataClasses;
using FruityFoundation.Base.Structures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages;

/// <summary>
/// Index Model
/// </summary>
public class IndexModel : PageModel
{
	private readonly LinkProvider _linkProvider;
	private readonly UserProvider _userProvider;

	/// <summary>
	/// C'tor
	/// </summary>
	public IndexModel(LinkProvider linkProvider, UserProvider userProvider)
	{
		_linkProvider = linkProvider;
		_userProvider = userProvider;
	}

	/// <summary>
	/// Search results
	/// </summary>
	public Maybe<IReadOnlyCollection<(Link Link, User Owner)>> SearchResults = Maybe.Empty<IReadOnlyCollection<(Link, User)>>();

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

		SearchResults = Maybe.Create<IReadOnlyCollection<(Link Link, User Owner)>>(linksWithOwners);
	}
}
