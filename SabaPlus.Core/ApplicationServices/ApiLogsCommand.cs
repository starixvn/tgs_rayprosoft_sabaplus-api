using MongoDB.Bson;

namespace SabaPlus.Core.ApplicationServices;

public record ApiLogsCommand(string Key, string Action, object Req, object Res);