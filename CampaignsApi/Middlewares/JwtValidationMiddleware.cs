using System.Security.Claims;
using Core.Models;

namespace CampaignsApi.Middlewares;

/// <summary>
/// Middleware para validação de tokens JWT via UserAPI.
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidationService tokenValidationService)
    {
        // Pular validação para endpoints públicos
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var token = ExtractTokenFromHeader(context.Request);
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token não fornecido para endpoint protegido: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Token de autorização requerido.");
            return;
        }

        var validationResult = await tokenValidationService.ValidateTokenAsync(token);
        
        if (validationResult == null || !validationResult.IsValid)
        {
            _logger.LogWarning("Token inválido para endpoint: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Token inválido ou expirado.");
            return;
        }

        // Adicionar claims do usuário ao contexto
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, validationResult.Username),
            new(ClaimTypes.Role, validationResult.Role),
            new(ClaimTypes.NameIdentifier, validationResult.UserId.ToString()),
            new("jti", validationResult.TokenId ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        context.User = new ClaimsPrincipal(identity);

        _logger.LogInformation("Usuário autenticado: {Username} com role: {Role}", 
            validationResult.Username, validationResult.Role);

        await _next(context);
    }

    /// <summary>
    /// Extrai o token JWT do header Authorization.
    /// </summary>
    /// <param name="request">Request HTTP.</param>
    /// <returns>Token JWT sem o prefixo 'Bearer ' ou null se não encontrado.</returns>
    private static string? ExtractTokenFromHeader(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    /// <summary>
    /// Verifica se o endpoint é público (não requer autenticação).
    /// </summary>
    /// <param name="path">Caminho da requisição.</param>
    /// <returns>True se for endpoint público.</returns>
    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/swagger",
            "/health",
            "/api-docs"
        };

        return publicPaths.Any(publicPath => 
            path.StartsWithSegments(publicPath, StringComparison.OrdinalIgnoreCase));
    }
}