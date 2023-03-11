using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using StreamFileApi.Model.Responses;
using Vostok.Logging.Abstractions;

namespace StreamFileApi.Middlewares;

internal class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate next;
    private readonly ExceptionHandlerOptions options;
    private readonly Func<object, Task> clearCacheHeadersDelegate;
    private readonly ILog log;

    public ExceptionHandlerMiddleware(RequestDelegate next, IOptions<ExceptionHandlerOptions> options, ILog log)
    {
        this.next = next;
        this.options = options.Value;
        clearCacheHeadersDelegate = ClearCacheHeaders;
        this.log = log;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                log.Warn("The response has already started, the error handler will not be executed.");
                throw;
            }

            var originalPath = context.Request.Path;

            try
            {
                context.Response.Clear();
                context.Response.OnStarting(clearCacheHeadersDelegate, context.Response);
                context.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();

                if (options.Responses.TryGetValue(ex.GetType(), out var exceptionResponse))
                {
                    context.Response.StatusCode = exceptionResponse.StatusCode;

                    await WriteResponseAsync(context, CreateErrorResponse(exceptionResponse.StatusCode, exceptionResponse.Message));

                    log.Warn(ex, $"An exception has occurred: {ex.Message}");
                }
                else
                {
                    const int statusCode = (int) HttpStatusCode.InternalServerError;
                    context.Response.StatusCode = statusCode;
                    await WriteResponseAsync(context, CreateErrorResponse(statusCode, options.DefaultErrorMessage));

                    log.Error(ex, $"An unhandled exception has occurred : {ex.Message}");
                }

                return;
            }
            catch (Exception ex2)
            {
                log.Error(ex2, "An exception was thrown attempting to execute the error handler.");
            }
            finally
            {
                context.Request.Path = originalPath;
            }
            throw;
        }
    }

    private async Task WriteResponseAsync(HttpContext context, object response)
    {
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response, options.SerializerSettings), Encoding.UTF8);
    }

    private static ApiError CreateErrorResponse(int statusCode, string message)
    {
        return new ApiError
        {
            StatusCode = statusCode,
            Message = message
        };
    }

    private static Task ClearCacheHeaders(object state)
    {
        var response = (HttpResponse)state;
        response.Headers[HeaderNames.CacheControl] = "no-cache";
        response.Headers[HeaderNames.Pragma] = "no-cache";
        response.Headers[HeaderNames.Expires] = "-1";
        response.Headers.Remove(HeaderNames.ETag);
        return Task.CompletedTask;
    }
}