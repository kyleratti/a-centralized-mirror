using System.Reflection;
using ApplicationData.Services;
using FruityFoundation.Base.Structures;
using Microsoft.Extensions.Caching.Memory;
using Nito.AsyncEx;

namespace BackgroundProcessor.Templates;

public class TemplateCache
{
	private readonly AsyncLock _lock = new();
	private Maybe<string> _commentReplyTemplate = Maybe.Empty<string>();

	public async ValueTask<string> GetCommentReplyTemplate(CancellationToken cancellationToken)
	{
		using var accessLock = await _lock.LockAsync(cancellationToken);

		if (_commentReplyTemplate.HasValue)
			return _commentReplyTemplate.Value;

		var assembly = Assembly.GetExecutingAssembly();
		const string resourcePath = "BackgroundProcessor.Templates.CommentReply.md";

		await using var stream = assembly.GetManifestResourceStream(resourcePath);

		if (stream is null)
			throw new InvalidOperationException($"Unable to find resource: {resourcePath}");

		using var reader = new StreamReader(stream);

		var templateString = await reader.ReadToEndAsync(cancellationToken);
		_commentReplyTemplate = templateString;

		return templateString;
	}
}
