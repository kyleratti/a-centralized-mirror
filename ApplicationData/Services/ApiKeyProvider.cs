﻿using DataClasses;
using FruityFoundation.Base.Structures;
using FruityFoundation.DataAccess.Abstractions;
using FruityFoundation.FsBase;

namespace ApplicationData.Services;

public class ApiKeyProvider
{
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public ApiKeyProvider(
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<Maybe<User>> FindUserByApiKey(string apiKey)
	{
		await using var connection = _dbConnectionFactory.CreateReadOnlyConnection();
		using var reader = await connection.ExecuteReader(
			@"SELECT u.user_id, u.display_username, u.developer_username, u.weight, u.created_at, u.updated_at, u.is_admin
				FROM users u
				INNER JOIN api_keys ak ON u.user_id = ak.user_id
				WHERE
					ak.api_key = @apiKey
					AND u.is_deleted = false",
			new { apiKey });

		if (!reader.Read())
			return Maybe.Empty<User>();

		return new User(
			userId: reader.GetInt32(0),
			displayUsername: reader.GetString(1),
			developerUsername: reader.GetString(2),
			weight: reader.GetInt32(3),
			createdAt: reader.GetDateTime(4),
			updatedAt: Option.fromMaybe(reader.TryGetDateTime(5)),
			isDeleted: false,
			isAdministrator: reader.GetBoolean(6)
		);
	}

	public async Task CreateApiKeyForUser(int userId, string apiKey)
	{
		await using var connection = _dbConnectionFactory.CreateConnection();
		await connection.Execute(
			@"INSERT INTO api_keys (
				api_key, created_at, last_used_at, user_id
			) VALUES (
				@apiKey, CURRENT_TIMESTAMP, NULL, @userId
			)",
			new { apiKey, userId });
	}
}
