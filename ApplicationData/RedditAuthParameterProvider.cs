using Core.AppSettings;
using Microsoft.Extensions.Options;
using SnooBrowser.Util;

namespace ApplicationData;

/// <summary>
/// RedditAuthParameterProvider
/// </summary>
public class RedditAuthParameterProvider : IAuthParameterProvider
{
	private readonly IOptions<RedditSettings> _redditSettings;

	/// <summary>
	/// C'tor
	/// </summary>
	public RedditAuthParameterProvider(IOptions<RedditSettings> redditSettings)
	{
		_redditSettings = redditSettings;
	}

	/// <inheritdoc />
	public string AppId => _redditSettings.Value.AppId;

	/// <inheritdoc />
	public string AppSecret => _redditSettings.Value.AppSecret;

	/// <inheritdoc />
	public string RefreshToken => _redditSettings.Value.RefreshToken;
}
