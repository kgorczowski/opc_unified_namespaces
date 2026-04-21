using System.Net;

namespace OPCGateway.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex, logger);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger<ExceptionHandlingMiddleware> logger)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError,
        };

        if (context.Response.StatusCode == (int)HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception has occurred.");
        }
        else
        {
            logger.LogWarning(exception, "An exception has occurred.");
        }

        var response = new
        {
            context.Response.StatusCode,
            exception.Message,
        };

        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}