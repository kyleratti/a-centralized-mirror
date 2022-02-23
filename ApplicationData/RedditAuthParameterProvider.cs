using Core.AppSettings;
using SnooBrowser.Util;

namespace ApplicationData;

/// <summary>
/// RedditAuthParameterProvider
/// </summary>
public class RedditAuthParameterProvider : IAuthParameterProvider
{
	private readonly RedditSettings _redditSettings;

	/// <summary>
	/// C'tor
	/// </summary>
	public RedditAuthParameterProvider(RedditSettings redditSettings)
	{
		_redditSettings = redditSettings;
	}

	/// <inheritdoc />
	public string AppId => _redditSettings.AppId;

	/// <inheritdoc />
	public string AppSecret => _redditSettings.AppSecret;

	/// <inheritdoc />
	public string RefreshToken => _redditSettings.RefreshToken;
}