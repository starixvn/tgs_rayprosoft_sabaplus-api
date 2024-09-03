using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SabaPlus.Repositories.Models;

public record ApiCallLog
{
	[BsonId]
	public ObjectId Id { get; set; } = default!;
	/// <summary>
	/// The operator identifier
	/// </summary>
	[BsonElement("time")]
	public DateTime Time { get; set; } = default!;

	[BsonElement("request")]
	public BsonDocument Request { get; set; } = default!;

	[BsonElement("response")]
	public BsonDocument Response { get; set; } = default!;
}