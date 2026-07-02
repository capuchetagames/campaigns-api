using Core.Models;
using Microsoft.Extensions.Primitives;

namespace CampaignsApi.Middlewares;

public class LogMiddleware
{
    private readonly RequestDelegate _next;
    private const string _correlationIdHeader = "X-Correlation-ID";
    public  const string ItemsKey      = "CorrelationId"; 
    public LogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdServiceService)
    {
        var correlationId = GetCorrelationId(context,  correlationIdServiceService);
        AddCorrelationIdHeaderToResponse(context, correlationId);

        return _next(context);
    }
    private StringValues GetCorrelationId(HttpContext context, ICorrelationIdService correlationIdServiceService)
    {
        if (context.Request.Headers.TryGetValue(_correlationIdHeader, out var correlationId))
        {
            correlationIdServiceService.Set(correlationId);
            context.Items[ItemsKey] = correlationId;
            return correlationId;
        }
        
        correlationId = Guid.NewGuid().ToString();
        
        correlationIdServiceService.Set(correlationId);
        context.Items[ItemsKey] = correlationId;
        
        return correlationId;
    }
    private void AddCorrelationIdHeaderToResponse(HttpContext context, StringValues correlationId)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_correlationIdHeader] = new []{correlationId.ToString()};
            return Task.CompletedTask;
        });
    }
}

public static class LogMiddlewareExtensions
{
    public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LogMiddleware>();
    }
}