namespace WebApi.Models;

/// <summary>
/// Unauthorized
/// </summary>
/// <param name="Message">The message describing the authorization failure.</param>
public record UnauthorizedResponse(string Message);