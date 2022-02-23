namespace WebApi.Models.Admin;

public record UserAdminModel(
	int UserId,
	string DisplayUsername,
	string DeveloperUsername,
	int Weight,
	DateTime CreatedAt,
	DateTime? UpdatedAt,
	bool IsDeleted,
	bool IsAdministrator
);