namespace Core.Dtos;

/// <summary>
/// DTO para requisição de validação de token JWT.
/// </summary>
public class TokenValidationRequestDto
{
    /// <summary>
    /// Token JWT a ser validado (sem o prefixo 'Bearer ').
    /// </summary>
    public string Token { get; set; } = string.Empty;
}