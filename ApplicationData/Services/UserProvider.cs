using System.Data;
using System.Data.Common;
using Dapper;
using DataClasses;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;

namespace ApplicationData.Services;

public class UserProvider
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public UserProvider(
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<IReadOnlyCollection<User>> GetAllUsers()
	{
		using var connection = await _dbConnectionFactory.CreateReadOnlyConnection();
		using var reader = await connection.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM users
				ORDER BY created_at");

		var users = new List<User>();

		while (reader.Read())
			users.Add(new User(
				userId: reader.GetInt32(0),
				displayUsername: reader.GetString(1),
				developerUsername: reader.GetString(2),
				weight: reader.GetInt32(3),
				createdAt: reader.GetDateTime(4),
				updatedAt: Option.fromMaybe(reader.TryGetDateTime(5)),
				isDeleted: reader.GetBoolean(6),
				isAdministrator: reader.GetBoolean(7)
			));

		return users;
	}

	public async Task<Maybe<User>> FindUserByDisplayName(string displayUsername)
	{
		using var connection = await _dbConnectionFactory.CreateReadOnlyConnection();
		using var reader = await connection.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM users
				WHERE display_username LIKE @displayUsername",
			new { displayUsername});

		if (!reader.Read())
			return Maybe.Empty<User>();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: Option.fromMaybe(reader.TryGetDateTime(5)),
			isDeleted: reader.GetBoolean(6),
			isAdministrator: reader.GetBoolean(7)
		);
	}

	public async Task<Maybe<User>> FindUserByIdIncludeDeleted(int userId)
	{
		using var connection = await _dbConnectionFactory.CreateReadOnlyConnection();
		using var reader = await connection.ExecuteReaderAsync(
			@"SELECT user_id, display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
				FROM users
				WHERE user_id = @userId",
			new { userId });

		if (!reader.Read())
			return Maybe.Empty<User>();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: Option.fromMaybe(reader.TryGetDateTime(5)),
			isDeleted: reader.GetBoolean(6),
			isAdministrator: reader.GetBoolean(7)
		);
	}

	public async Task<int> CreateUser(NewUser newUser)
	{
		using var connection = await _dbConnectionFactory.CreateConnection();
		return await connection.ExecuteScalarAsync<int>(
			@"INSERT INTO users (
				display_username, developer_username, weight, created_at, updated_at, is_deleted, is_admin
			) VALUES (
				@displayUsername, @developerUsername, @weight, CURRENT_TIMESTAMP, NULL, false, @isAdmin
			) RETURNING user_id",
			new
			{
				displayUsername = newUser.DisplayUsername,
				developerUsername = newUser.DeveloperUsername,
				weight = Option.toMaybe(newUser.Weight).OrValue(0),
				isAdmin = newUser.IsAdministrator,
			});
	}

	public async Task DeleteUserById(int userId)
	{
		using var connection = await _dbConnectionFactory.CreateConnection();
		await connection.ExecuteAsync(
			@"UPDATE users SET is_deleted = true WHERE user_id = @userId",
			new { userId });
	}
}
