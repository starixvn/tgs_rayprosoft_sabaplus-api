using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SabaPlus.API.Middlewares.Attributes;

public class ReadableBodyAttribute : Attribute, IAuthorizationFilter
{
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var bodyStr = "";
		var req = context.HttpContext.Request;

		// Arguments: Stream, Encoding, detect encoding, buffer size
		// AND, the most important: keep stream opened
		using (StreamReader reader
				  = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
		{
			bodyStr = reader.ReadToEnd();
		}

		// Rewind, so the core is not lost when it looks at the body for the request
		req.Body.Position = 0;

		SentrySdk.CaptureMessage(bodyStr);
		Console.WriteLine(bodyStr);
	}
}