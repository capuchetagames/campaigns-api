using System.Text;
using System.Text.Json;
using Core.Dtos;
using Core.Models;

namespace CampaignsApi.Service;

public class TokenValidationService : ITokenValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TokenValidationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TokenValidationService(IHttpClientFactory httpClientFactory, ILogger<TokenValidationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AccountApi");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Valida um token JWT fazendo uma chamada para o UserAPI.
    /// </summary>
    /// <param name="token">Token JWT a ser validado (sem o prefixo 'Bearer ').</param>
    /// <returns>Resposta da validação contendo informações do usuário se válido.</returns>
    public async Task<TokenValidationResponseDto?> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token vazio fornecido para validação.");
                return null;
            }

            var request = new TokenValidationRequestDto { Token = token };
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Enviando token para validação no UserAPI.");
            
            var response = await _httpClient.PostAsync("auth/validate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var validationResponse = JsonSerializer.Deserialize<TokenValidationResponseDto>(responseContent, _jsonOptions);
                
                _logger.LogInformation($"Token validado com sucesso para usuário: {validationResponse?.Id}");
                return validationResponse;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Falha na validação do token. Status: {response.StatusCode}, Erro: {errorContent}");
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Erro de comunicação com UserAPI: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"Timeout na comunicação com UserAPI: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Erro ao deserializar resposta do UserAPI: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro inesperado durante validação de token: {ex.Message}");
            return null;
        }
    }
}