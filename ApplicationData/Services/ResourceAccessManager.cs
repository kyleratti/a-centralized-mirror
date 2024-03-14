using Microsoft.Extensions.Caching.Memory;
using Nito.AsyncEx;

namespace ApplicationData.Services;

public sealed class ResourceAccessManager(IMemoryCache _memoryCache)
{
	private readonly AsyncLock _resourceWriteLock = new();

	public async Task<IDisposable> ObtainExclusiveAccess(string resourceId, CancellationToken cancellationToken)
	{
		var resourceLock = await GetOrCreateResourceLock(resourceId, cancellationToken);
		return await resourceLock.LockAsync(cancellationToken);
	}

	public async Task<T> WithExclusiveAccess<T>(string resourceId, Func<Task<T>> action, CancellationToken cancellationToken)
	{
		var resourceLock = await GetOrCreateResourceLock(resourceId, cancellationToken);
		using var _ = await resourceLock.LockAsync(cancellationToken);

		return await action();
	}

	public async Task WithExclusiveAccess(string resourceId, Func<Task> action, CancellationToken cancellationToken) =>
		await WithExclusiveAccess(
			resourceId,
			async () =>
			{
				await action();
				return 0;
			},
			cancellationToken);

	private async Task<AsyncLock> GetOrCreateResourceLock(string resourceId, CancellationToken cancellationToken)
	{
		using var _ = await _resourceWriteLock.LockAsync(cancellationToken);

		var resourceLock = _memoryCache.GetOrCreate(
			resourceId,
			entry =>
			{
				entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));
				return new AsyncLock();
			});

		if (resourceLock is null)
			throw new InvalidOperationException($"Resource lock was not created or retrieved (ResourceId={resourceId}");

		return resourceLock;
	}
}
