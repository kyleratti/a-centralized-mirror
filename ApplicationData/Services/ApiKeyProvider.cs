using Core.DbConnection;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;
using Npgsql;

namespace ApplicationData.Services;

public class ApiKeyProvider
{
	private readonly DbConnection _db;

	public ApiKeyProvider(
		DbConnection db
	)
	{
		_db = db;
	}

	public async Task<Maybe<User>> FindUserByApiKey(string apiKey)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT u.user_id, u.display_username, u.developer_username, u.weight, u.created_at, u.updated_at, u.is_admin
				FROM app.users u
				INNER JOIN app.api_keys ak ON u.user_id = ak.user_id
				WHERE
					ak.api_key = @apiKey
					AND u.is_deleted = false",
			new NpgsqlParameter[]
			{
				new("@apiKey", apiKey)
			}
		);

		if (!await reader.ReadAsync())
			return Maybe<User>.Empty();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: reader.TryGetDateTime(5).ToFSharpOption(),
			isDeleted: false,
			isAdministrator: reader.GetBoolean(6)
		);
	}

	public async Task<Maybe<int>> FindUserIdByApiKey(string apiKey)
	{
		await using var reader = await _db.ExecuteReaderAsync(
			@"SELECT u.user_id
				FROM app.users u
				INNER JOIN app.api_keys ak ON u.user_id = ak.user_id
				WHERE ak.api_key = @apiKey",
			new NpgsqlParameter[]
			{
				new("@apiKey", apiKey)
			}
		);

		if (!await reader.ReadAsync())
			return Maybe<int>.Empty();

		return reader.GetInt32(0);
	}

	public async Task CreateApiKeyForUser(int userId, string apiKey) =>
		await _db.ExecuteSqlNonQueryAsync(
			@"INSERT INTO app.api_keys (
				api_key, created_at, last_used_at, user_id
			) VALUES (
				@apiKey, NOW(), NULL, @userId
			)",
			new NpgsqlParameter[]
			{
				new("@apiKey", apiKey),
				new("@userId", userId)
			});
}