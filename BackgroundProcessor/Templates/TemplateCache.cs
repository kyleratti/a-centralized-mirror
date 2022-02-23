using System.Reflection;
using ApplicationData.Services;
using Microsoft.Extensions.Caching.Memory;

namespace BackgroundProcessor.Templates;

public class TemplateCache
{
	private readonly GlobalMemoryCache _globalCache;

	public TemplateCache(GlobalMemoryCache globalCache)
	{
		_globalCache = globalCache;
	}

	public async Task<string> GetCommentReply() => await GetResourceByName("CommentReply.md");


	private async Task<string> GetResourceByName(string name) =>
		await _globalCache.GetOrCreateAsync(CreateCacheKey(name), async _ => await GetResourceByNameWithoutCache(name));

	private static string CreateCacheKey(string key) => $"{nameof(TemplateCache)}.{key}";

	private static async Task<string> GetResourceByNameWithoutCache(string name)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var resourcePath = $"{nameof(BackgroundProcessor)}.{nameof(Templates)}.{name}";

		await using var stream = assembly.GetManifestResourceStream(resourcePath);

		if (stream is null)
			throw new InvalidOperationException($"Unable to find resource: {resourcePath}");

		using var reader = new StreamReader(stream);

		return await reader.ReadToEndAsync();
	}
}