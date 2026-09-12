using log4net;
using System.Text;

namespace PartnerApi.Logging
{
    public class Log4NetLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Log4NetLoggingMiddleware));

        public Log4NetLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestId = Guid.NewGuid().ToString("N")[..12];
            context.Response.Headers["X-Correlation-ID"] = requestId;

            // 1. Read & Encrypt Request Payload for Log File
            context.Request.EnableBuffering();
            string rawRequestBody = string.Empty;

            if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
            {
                using var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                rawRequestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            string sanitizedRequestBody = LogEncryptionHelper.SanitizeJsonPayload(rawRequestBody);

            Log.Info($"[REQ-{requestId}] Method: {context.Request.Method} | Path: {context.Request.Path} | IP: {context.Connection.RemoteIpAddress}");
            Log.Info($"[REQ-{requestId}] Request Body: {sanitizedRequestBody}");

            // 2. Intercept & Log Response Payload
            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyMemoryStream = new MemoryStream();
            context.Response.Body = responseBodyMemoryStream;

            try
            {
                await _next(context);

                responseBodyMemoryStream.Position = 0;
                string responseBodyText = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();
                responseBodyMemoryStream.Position = 0;

                string sanitizedResponseBody = LogEncryptionHelper.SanitizeJsonPayload(responseBodyText);

                Log.Info($"[RESP-{requestId}] Status: {context.Response.StatusCode}");
                Log.Info($"[RESP-{requestId}] Response Body: {sanitizedResponseBody}");

                await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);
            }
            catch (Exception ex)
            {
                Log.Error($"[ERR-{requestId}] Unhandled Exception on path: {context.Request.Path}", ex);
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }
    }
}
