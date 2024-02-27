﻿using System.Data;
using Dapper;
using DataClasses;
using FruityFoundation.Base.Extensions;
using FruityFoundation.Base.Structures;
using FruityFoundation.FsBase;

namespace ApplicationData.Services;

public class ApiKeyProvider
{
	private readonly IDbConnection _db;

	public ApiKeyProvider(
		IDbConnection db
	)
	{
		_db = db;
	}

	public async Task<Maybe<User>> FindUserByApiKey(string apiKey)
	{
		var reader = await _db.ExecuteReaderAsync(
			@"SELECT u.user_id, u.display_username, u.developer_username, u.weight, u.created_at, u.updated_at, u.is_admin
				FROM app.users u
				INNER JOIN app.api_keys ak ON u.user_id = ak.user_id
				WHERE
					ak.api_key = @apiKey
					AND u.is_deleted = false",
			new { apiKey });

		if (!reader.Read())
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

	public async Task CreateApiKeyForUser(int userId, string apiKey) =>
		await _db.ExecuteAsync(
			@"INSERT INTO app.api_keys (
				api_key, created_at, last_used_at, user_id
			) VALUES (
				@apiKey, NOW(), NULL, @userId
			)",
			new { apiKey, userId });
}
