using System.IO.Compression;

namespace SabaPlus.API.Middlewares
{
    public class DecompressionMiddleware
    {
        private readonly RequestDelegate _next;

        public DecompressionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == HttpMethods.Post &&
                context.Request.Headers["Content-Encoding"].Any(h => h.Contains("gzip")))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(context.Request.Body, CompressionMode.Decompress))
                    {
                        await gzipStream.CopyToAsync(decompressedStream);
                    }
                    decompressedStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = decompressedStream;
					Console.WriteLine($"Request Body - {decompressedStream}");
                }
            }

            await _next(context);
        }
    }
}