using System.Collections;
using System.Net;

#pragma warning disable CS1591

namespace WebApi.Middleware;

/// <summary>
/// The unexpected error.
/// </summary>
/// <param name="Message">The message of the error.</param>
/// <param name="StackTrace">The stack trace of the error.</param>
/// <param name="Data">A dictionary of data included with the exception, if any.</param>
/// <param name="InnerException">The inner exception of the same <see cref="CapturedException"/> type, recursive, or <c>null</c> if there isn't one.</param>
internal record CapturedException(
	string Message,
	string? StackTrace,
	IDictionary Data,
	CapturedException? InnerException
);

public class JsonExceptionMiddleware
{
	private readonly RequestDelegate _next;

	public JsonExceptionMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext httpContext)
	{
		try
		{
			await _next(httpContext);
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(httpContext, ex);
		}
	}

	public static async Task<T> InvokeWithExceptionHandler<T>(HttpContext httpContext, Func<Task<T>> action)
	{
		try
		{
			return await action();
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(httpContext, ex);
			throw;
		}
	}

	private static async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
	{
		HttpStatusCode GetStatusCode()
		{
			if (ex.Data.Contains("HttpStatusCode") && ex.Data["HttpStatusCode"] is HttpStatusCode)
			{
				var statusCode = (HttpStatusCode)ex.Data["HttpStatusCode"]!;
				ex.Data.Remove("HttpStatusCode"); // No need to leak this to the client.
				return statusCode;
			}

			return HttpStatusCode.InternalServerError;
		}

		var statusCode = (int)GetStatusCode();

		httpContext.Response.ContentType = "application/json";
		httpContext.Response.StatusCode = statusCode;
		await httpContext.Response.WriteAsJsonAsync(BuildCapturedException(ex));
	}

	private static CapturedException? BuildCapturedException(Exception? ex) =>
		ex is null
			? null
			: new CapturedException(ex.Message, ex.StackTrace, ex.Data, BuildCapturedException(ex.InnerException));
}

public static class JsonExceptionMiddlewareExtensions
{
	public static IApplicationBuilder UseJsonExceptionHandler(this IApplicationBuilder builder) =>
		builder.UseMiddleware<JsonExceptionMiddleware>();
}