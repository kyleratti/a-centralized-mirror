namespace WebApi.Options;

/// <summary>
/// Hosting Options
/// </summary>
public class HostingOptions
{
	/// <summary>
	/// Enable the HTTPS redirection middleware.
	/// </summary>
	public bool EnableHttpsRedirect { get; set; } = true;
}
