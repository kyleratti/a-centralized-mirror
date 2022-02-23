using Microsoft.Extensions.Caching.Memory;

namespace ApplicationData.Services;

public class GlobalMemoryCache : IMemoryCache
{
	private readonly IMemoryCache _memoryCache;

	public GlobalMemoryCache(IMemoryCache memoryCache)
	{
		_memoryCache = memoryCache;
	}

	/// <inheritdoc />
	public void Dispose() => _memoryCache.Dispose();

	/// <inheritdoc />
	public bool TryGetValue(object key, out object value) => _memoryCache.TryGetValue(key, out value);

	/// <inheritdoc />
	public ICacheEntry CreateEntry(object key) => _memoryCache.CreateEntry(key);

	/// <inheritdoc />
	public void Remove(object key) => _memoryCache.Remove(key);
}