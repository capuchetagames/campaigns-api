namespace Core.Dtos;

/// <summary>
/// DTO para resposta de validação de token JWT.
/// </summary>
public class TokenValidationResponseDto
{
    /// <summary>
    /// Indica se o token é válido.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Nome de usuário extraído do token.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Role/permissão do usuário extraída do token.
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// ID único do token (JTI claim).
    /// </summary>
    public string? TokenId { get; set; }
}