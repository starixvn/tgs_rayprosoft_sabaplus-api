using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using SabaPlus.Core;
using SabaPlus.Repositories.Models;

namespace SabaPlus.Repositories;

internal class ApiLogsRepository(IMongoClient client) : IApiLogsRepository
{
	private static readonly JsonSerializerOptions _WriteOptions = new()
	{
		WriteIndented = true
	};

	public async ValueTask<bool> AddAsync(string action, Object req, Object res, CancellationToken cancellationToken = default)
	{
		var collection = GetCollection(action);
		var document = new ApiCallLog()
		{
			Time = DateTime.UtcNow,
			Request = BsonDocument.Parse(JsonSerializer.Serialize(req, _WriteOptions)),
			Response = BsonDocument.Parse(JsonSerializer.Serialize(res, _WriteOptions))
		};

		await collection.InsertOneAsync(
			document: document,
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return true;
	}

	private IMongoCollection<ApiCallLog> GetCollection(string action)
	=> client
	.GetDatabase($"sabaplus_logs_master")
	.GetCollection<ApiCallLog>($"{action.ToLower()}_logs");
}