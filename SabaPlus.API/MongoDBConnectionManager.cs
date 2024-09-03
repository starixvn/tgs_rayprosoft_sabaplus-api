using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SabaPlus.API;

internal class MongoDBConnectionManager
{
    public IMongoClient MongoClient { get; }

    public MongoDBConnectionManager(
        IOptions<MongoDBOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var mongoUrl = new MongoUrl(options.ConnectionString);
        MongoClient = new MongoClient(mongoUrl);
    }
}