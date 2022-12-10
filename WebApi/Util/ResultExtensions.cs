using ResultMonad;

namespace WebApi.Util;

/// <summary>
/// ResultMonad extensions
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Return true if the Result is a successful one.
	/// Assign success and error out parameters.
	/// </summary>
	public static bool TrySuccess<TSuccess, TError>(this Result<TSuccess, TError> item, out TSuccess? success,
		out TError? error)
	{
		if (item.IsSuccess)
		{
			success = item.Value;
			error = default;
		}
		else
		{
			success = default;
			error = item.Error;
		}

		return item.IsSuccess;
	}
}