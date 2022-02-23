using Core.DbConnection;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;
using Npgsql;

namespace ApplicationData.Services;

public class UserProvider
{
	private readonly DbConnection _db;

	public UserProvider(
		DbConnection dbConnection
	)
	{
		_db = dbConnection;
	}

	public async Task<IReadOnlyCollection<User>> GetAllUsers()
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM app.users
				ORDER BY created_at", Array.Empty<NpgsqlParameter>());

		var users = new List<User>();

		while (await reader.ReadAsync())
			users.Add(new User(
				userId: reader.GetInt32(0),
				displayUsername: reader.GetString(1),
				developerUsername: reader.GetString(2),
				weight: reader.GetInt32(3),
				createdAt: reader.GetDateTime(4),
				updatedAt: reader.TryGetDateTime(5).ToFSharpOption(),
				isDeleted: reader.GetBoolean(6),
				isAdministrator: reader.GetBoolean(7)
			));

		return users;
	}

	public async Task<Maybe<User>> FindUserByDisplayName(string displayUsername)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM app.users
				WHERE display_username ILIKE @displayUsername",
			new NpgsqlParameter[]
			{
				new("@displayUsername", displayUsername)
			});

		if (!await reader.ReadAsync())
			return Maybe<User>.Empty();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: reader.TryGetDateTime(5).ToFSharpOption(),
			isDeleted: reader.GetBoolean(6),
			isAdministrator: reader.GetBoolean(7)
		);
	}

	public async Task<Maybe<User>> FindUserByIdIncludeDeleted(int userId)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM app.users
				WHERE user_id = @userId",
			new NpgsqlParameter[]
			{
				new("@userId", userId)
			});

		if (!await reader.ReadAsync())
			return Maybe<User>.Empty();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: reader.TryGetDateTime(5).ToFSharpOption(),
			isDeleted: reader.GetBoolean(6),
			isAdministrator: reader.GetBoolean(7)
		);
	}

	public async Task<int> CreateUser(NewUser newUser) =>
		(int)await _db.ExecuteScalarAsync(
			@"INSERT INTO app.users (
				display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
			) VALUES (
				@displayUsername, @developerUsername, @weight, NOW(), NULL, false, @isAdmin
			) RETURNING user_id",
			new NpgsqlParameter[]
			{
				new("@displayUsername", newUser.DisplayUsername),
				new("@developerUsername", newUser.DeveloperUsername),
				new("@weight", newUser.Weight.ToMaybe().OrValue(0)),
				new("@isAdmin", newUser.IsAdministrator)
			});

	public async Task DeleteUserById(int userId) =>
		await _db.ExecuteSqlNonQueryAsync(
			@"UPDATE app.users SET is_deleted = true WHERE user_id = @userId",
			new NpgsqlParameter[]
			{
				new("@userId", userId)
			});
}