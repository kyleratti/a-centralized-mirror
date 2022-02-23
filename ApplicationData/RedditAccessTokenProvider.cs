using FruityFoundation.Base.Structures;
using SnooBrowser.Models;
using SnooBrowser.Util;

namespace ApplicationData;

public class RedditAccessTokenProvider : IAccessTokenProvider
{
	/// <inheritdoc />
	public Maybe<AccessToken> AccessToken { get; private set; } = Maybe<AccessToken>.Empty();

	/// <inheritdoc />
	public Func<AccessToken, Task> OnAccessTokenChanged => newAccessToken =>
	{
		AccessToken = newAccessToken;
		return Task.CompletedTask;
	};
}