using MongoDB.Bson;

namespace SabaPlus.Core.ApplicationServices;

public record ApiLogsCommand(string Action, object Req, object Res);