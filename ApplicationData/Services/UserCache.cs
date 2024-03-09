using DataClasses;
using FruityFoundation.Base.Structures;
using Microsoft.Extensions.Caching.Memory;

namespace ApplicationData.Services;

public class UserCache
{
	private readonly GlobalMemoryCache _globalCache;
	private readonly UserProvider _userProvider;

	public UserCache(
		GlobalMemoryCache globalCache,
		UserProvider userProvider
	)
	{
		_globalCache = globalCache;
		_userProvider = userProvider;
	}

	public Maybe<User> FindUserCacheOnly(int userId)
	{
		if (!_globalCache.TryGetValue(UserIdToKey(userId), out User? user))
			return Maybe.Empty<User>();

		return user!;
	}

	public async Task<Maybe<User>> FindUser(int userId)
	{
		var key = UserIdToKey(userId);
		if (_globalCache.TryGetValue(key, out User? value))
			return value!;

		var maybeUser = await _userProvider.FindUserByIdIncludeDeleted(userId);
		
		if (!maybeUser.Try(out var user))
			return Maybe.Empty<User>();

		return _globalCache.Set(UserIdToKey(userId), user, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5));
	}

	public User GetUserOrFail(int userId)
	{
		if (FindUserCacheOnly(userId).Try(out var user))
			return user;

		throw new KeyNotFoundException($"UserId not found in cache: #{userId}");
	}

	public void AddOrUpdateUser(User user) => _globalCache.Set(UserIdToKey(user.UserId), user);

	private static string UserIdToKey(int userId) => CreateGlobalCacheKey($"user.{userId}");

	private static string CreateGlobalCacheKey(string key) => $"{nameof(UserCache)}.{key}";
}
