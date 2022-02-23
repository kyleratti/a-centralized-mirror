using ApplicationData.Services;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;

#pragma warning disable CS1591
namespace WebApi.Controllers.Admin;

[Route("v1/admin/user")]
public class AdminUserController : AdminApiController
{
	private readonly UserProvider _userProvider;

	/// <inheritdoc />
	public AdminUserController(
		IServiceProvider serviceProvider,
		UserProvider userProvider
	) : base(serviceProvider)
	{
		_userProvider = userProvider;
	}

	[HttpGet]
	[Route("all")]
	public async Task<IReadOnlyCollection<UserAdminModel>> GetAllUsersAsync() =>
		(await _userProvider.GetAllUsers())
		.Select(x => new UserAdminModel(
			x.UserId,
			x.DisplayUsername,
			x.DeveloperUsername,
			x.Weight,
			x.CreatedAt,
			x.UpdatedAt.ToMaybe().ToNullable(),
			x.IsDeleted,
			x.IsAdministrator
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
			newUser.DisplayUsername,
			newUser.DeveloperUsername,
			newUser.Weight.ToMaybe().ToFSharpOption(),
			newUser.IsAdministrator.ToMaybe().OrValue(false)
		));

		return new ObjectResult((await _userProvider.FindUserByIdIncludeDeleted(userId)).Value);
	}

	[HttpDelete]
	[Route("{userId:int}")]
	public async Task<IActionResult> DeleteUserAsync(int userId, [FromQuery(Name = "allowDeleteAdmin")] bool allowDeleteAdmin)
	{
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

		if (existingUser.UserId == UserId)
			return new BadRequestObjectResult(new
			{
				message = "You cannot delete yourself."
			});

		await _userProvider.DeleteUserById(userId);

		return new OkResult();
	}
}