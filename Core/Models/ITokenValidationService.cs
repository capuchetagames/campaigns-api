using Core.Dtos;

namespace Core.Models;

/// <summary>
/// Interface para validação de tokens JWT via UserAPI.
/// </summary>
public interface ITokenValidationService
{
    /// <summary>
    /// Valida um token JWT fazendo uma chamada para o UserAPI.
    /// </summary>
    /// <param name="token">Token JWT a ser validado (sem o prefixo 'Bearer ').</param>
    /// <returns>Resposta da validação contendo informações do usuário se válido.</returns>
    Task<TokenValidationResponseDto?> ValidateTokenAsync(string token);
}