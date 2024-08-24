using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages;

/// <summary>
/// Error model
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
	/// <summary>
	/// Request id
	/// </summary>
	public string? RequestId { get; set; }

	/// <summary>
	/// Show request id
	/// </summary>
	public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

	/// <summary>
	/// GET
	/// </summary>
	public void OnGet()
	{
		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
	}
}
