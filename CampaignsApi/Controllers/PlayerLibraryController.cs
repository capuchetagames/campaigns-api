using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CatalogApi.Controllers;

/// <summary>
/// Gerencia a biblioteca de jogos dos usuários.
/// Utiliza autenticação distribuída via UserAPI.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlayerLibraryController : ControllerBase
{
    private readonly ILogger<PlayerLibraryController> _logger;

    public PlayerLibraryController(ILogger<PlayerLibraryController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Obtém a biblioteca de jogos do usuário autenticado.
    /// </summary>
    /// <returns>Lista de jogos na biblioteca do usuário.</returns>
    [HttpGet("my-games")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMyGames()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation($"Usuário {username} acessando sua biblioteca de jogos.");

        // Simulação de biblioteca de jogos do usuário
        //ToDo replace it with the database data
        var userLibrary = new[]
        {
            new { Id = 1, Name = "Cyberpunk 2077", PurchaseDate = DateTime.UtcNow.AddDays(-30) },
            new { Id = 2, Name = "The Witcher 3", PurchaseDate = DateTime.UtcNow.AddDays(-60) }
        };

        return Ok(new 
        { 
            UserId = userId,
            Username = username,
            Games = userLibrary,
            TotalGames = userLibrary.Length
        });
    }

    /// <summary>
    /// Endpoint público para verificar status do serviço de biblioteca.
    /// </summary>
    /// <returns>Status do serviço.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            service = "CatalogAPI - Player Library", 
            timestamp = DateTime.UtcNow 
        });
    }
}