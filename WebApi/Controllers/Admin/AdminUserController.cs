using ApplicationData.Services;
using DataClasses;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
using WebApi.Util;

#pragma warning disable CS1591
namespace WebApi.Controllers.Admin;

[Authorize(Roles = UserRoles.AdministratorRole)]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("v1/admin/user")]
public class AdminUserController : Controller
{
	private readonly UserProvider _userProvider;

	/// <inheritdoc />
	public AdminUserController(
		UserProvider userProvider
	)
	{
		_userProvider = userProvider;
	}

	[HttpGet]
	[Route("all")]
	public async Task<IReadOnlyCollection<UserAdminModel>> GetAllUsersAsync() =>
		(await _userProvider.GetAllUsers())
		.Select(x => new UserAdminModel(
			UserId: x.UserId,
			DisplayUsername: x.DisplayUsername,
			DeveloperUsername: x.DeveloperUsername,
			Weight: x.Weight,
			CreatedAt: x.CreatedAt,
			UpdatedAt: Option.toMaybe(x.UpdatedAt).ToNullable(),
			IsDeleted: x.IsDeleted,
			IsAdministrator: x.IsAdministrator
		))
		.ToArray();

	[HttpPost]
	[Route("")]
	public async Task<IActionResult> CreateUserAsync(SubmitUserModel newUser)
	{
		if ((await _userProvider.FindUserByDisplayName(newUser.DisplayUsername!)).Try(out var existingUser))
			return new ConflictObjectResult(new
			{
				message = $"A user with this display username already exists (#{existingUser.UserId}): {newUser.DisplayUsername}"
			});

		var userId = await _userProvider.CreateUser(new NewUser(
			displayUsername: newUser.DisplayUsername,
			developerUsername: newUser.DeveloperUsername,
			weight: Option.fromMaybe(newUser.Weight.ToMaybe()),
			isAdministrator: newUser.IsAdministrator.ToMaybe().OrValue(false)
		));

		return new ObjectResult((await _userProvider.FindUserByIdIncludeDeleted(userId)).Value);
	}

	[HttpDelete]
	[Route("{userId:int}")]
	public async Task<IActionResult> DeleteUserAsync(int userId, [FromQuery(Name = "allowDeleteAdmin")] bool allowDeleteAdmin)
	{
		if (!User.GetUserId().Try(out var loggedInUserId))
			throw new UnauthorizedAccessException("You must be logged in to delete a user.");

		if (!(await _userProvider.FindUserByIdIncludeDeleted(userId)).Try(out var existingUser))
			return new NotFoundObjectResult(new
			{
				message = $"A user with this ID was not found: {userId}"
			});

		if (existingUser.IsDeleted)
			return new BadRequestObjectResult(new
			{
				message = "This user has already been deleted."
			});

		if (existingUser.IsAdministrator && !allowDeleteAdmin)
			return new BadRequestObjectResult(new
			{
				message = "This user is an administrator. In order to delete them, please set the 'allowDeleteAdmin' query parameter to true."
			});

		if (existingUser.UserId == loggedInUserId)
			return new BadRequestObjectResult(new
			{
				message = "You cannot delete yourself."
			});

		await _userProvider.DeleteUserById(userId);

		return new OkResult();
	}
}
