using System.Diagnostics;

namespace CampaignsApi.Middlewares;

public class DynamoLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DynamoLoggingMiddleware> _logger;

    public DynamoLoggingMiddleware(RequestDelegate next, ILogger<DynamoLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw      = Stopwatch.StartNew();
        var request = context.Request;
        var correlationId = context.Items[LogMiddleware.ItemsKey]?.ToString() ?? "-";

        // ── dados da requisição ──────────────────────────
        var method = request.Method;
        var path   = request.Path;
        var query  = request.QueryString;
        var ip     = context.Connection.RemoteIpAddress?.ToString();


        // usa scope para o correlationId aparecer em TODOS os logs dessa requisição
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });
        
        _logger.LogInformation("→ {Method} {Path} | CorrelationId: {CorrelationId} | IP: {Ip}", method, path, correlationId, ip);

        try
        {
            // passa para o próximo middleware / controller
            await _next(context);

            sw.Stop();

            var status = context.Response.StatusCode;

            // loga diferente dependendo do status
            if (status >= 500)
                _logger.LogError("← {Method} {Path} | Status: {Status} | {Ms}ms", method, path, status, sw.ElapsedMilliseconds);

            else if (status >= 400)
                _logger.LogWarning("← {Method} {Path} | Status: {Status} | {Ms}ms", method, path, status, sw.ElapsedMilliseconds);

            else
                _logger.LogInformation("← {Method} {Path} | Status: {Status} | {Ms}ms", method, path, status, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();

            // exceções não tratadas — loga e deixa subir
            _logger.LogError(ex, "✗ {Method} {Path} | Exceção após {Ms}ms", method, path, sw.ElapsedMilliseconds);

            throw;
        }
    }
}