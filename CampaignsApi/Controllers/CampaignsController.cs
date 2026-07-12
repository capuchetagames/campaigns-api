using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core;
using Core.Dtos;
using Core.Entity;
using Core.Enums;
using Core.Models;
using Core.Models.ElasticSearch;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampaignsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CampaignsController> _logger;
    private readonly IElasticClient<Campaign> _elasticClient;
    private readonly IValidator<CampaignInput> _campaignInputValidator;
    private readonly IValidator<CampaignUpdateInput> _campaignUpdateValidator;

    private const string CampaingListCacheKey = "campaing-list";

    public CampaignsController(ICampaignRepository campaignRepository, ICacheService cacheService, ILogger<CampaignsController> logger,
        IRabbitMqService rabbitMqService, IElasticClient<Campaign> elasticClient, IValidator<CampaignInput> campaignInputValidator,
        IValidator<CampaignUpdateInput> campaignUpdateValidator)
    {
        _campaignRepository = campaignRepository;
        _cacheService = cacheService;
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _elasticClient = elasticClient;
        _campaignInputValidator = campaignInputValidator;
        _campaignUpdateValidator = campaignUpdateValidator;
    }

    /// <summary>
    /// Painel de Transparência: lista pública das campanhas ativas com o valor arrecadado.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetPublicActiveCampaigns()
    {
        try
        {
            var campaigns = _campaignRepository.GetAll()
                .Where(c => c.Status == Status.Active)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.FinancialGoal,
                    c.AmountRaised
                })
                .ToList();

            return Ok(campaigns);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao buscar campanhas ativas: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }

    [HttpGet]
    [Authorize(Policy = nameof(Role.Manager))]
    [ProducesResponseType(typeof(IEnumerable<Campaign>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation($"Usuário {username} acessando lista de Camapanhas.");

            var cachedGameList = await _cacheService.GetAsync<List<Campaign>>(CampaingListCacheKey);

            if (cachedGameList != null)
            {
                return Ok(cachedGameList);
            }
            
            var campaigns = _campaignRepository.GetAll();

            if (campaigns.Count > 0)
            {
                await _cacheService.SetAsync(CampaingListCacheKey, campaigns, TimeSpan.FromMinutes(15));
            } 
            
            _logger.LogInformation($"Retornados {campaigns.Count} campanhas {username}.");
            return Ok(campaigns);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao buscar lista de campanhas: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Ocorreu um erro interno ao buscar os jogos.",
                error = e.Message
            });
        }
    }
    
    
    private Guid ValidateUserToken(Role validateRole)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var role = new Role();
            
        if(!string.IsNullOrEmpty(userRole))
        {
            role = Enum.Parse<Role>(userRole);
        }
            
        //var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        var userGuid = Guid.Empty;
            
        if(!string.IsNullOrEmpty(userId) || role == validateRole)
        {
            userGuid = Guid.Parse(userId);
        }
        
        return userGuid;
    }

    [HttpGet("{id:Guid}")]
    [Authorize(Policy = nameof(Role.Manager))]
    [ProducesResponseType(typeof(Campaign), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        try
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogInformation($"Usuário {username} buscando Campaign ID: {id}");

            var campaignKey = $"campaign-{id}";
            
            var cachedGame = await _cacheService.GetAsync<Campaign>(campaignKey);
            
            if (cachedGame != null)
            {
                return Ok(cachedGame);
            }

            var game = _campaignRepository.GetById(id);
            
            if (game == null)
            {
                return NotFound(new { message = $"Campanha com ID {id} não encontrado." });
            }
            
            await _cacheService.SetAsync(campaignKey, game, TimeSpan.FromMinutes(15));
            
            _logger.LogInformation($"Campanha {id} ({game.Title}) retornado para usuário {username}");
            return Ok(game);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao buscar campanha {id}: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }

    [HttpPost]
    [Authorize(Policy = nameof(Role.Manager))]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(Campaign), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] CampaignInput campaignInput)
    {
        try
        {
            var userGuid = ValidateUserToken(Role.Manager);
            
            if(userGuid == Guid.Empty)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                return Unauthorized($"user: {userGuid} userRole: {userRole}");
            }
            _logger.LogInformation($"User: {userGuid}  criando nova Campanha: {campaignInput.Title}");
            
            
            var validationResult = await _campaignInputValidator.ValidateAsync(campaignInput);
            
            if(!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToString());
            }
            

            var campaign = new Campaign()
            {
                Title = campaignInput.Title,
                Description = campaignInput.Description,
                StartDate = campaignInput.StartDate,
                EndDate = campaignInput.EndDate,
                FinancialGoal = campaignInput.FinancialGoal,
                Status = CheckStatusByDate(campaignInput.StartDate)
                
            };
            
            _campaignRepository.Add(campaign);
            
            
            await _rabbitMqService.PublishAsync(
                "campaign.events",
                "campaign.added",
                new NewCampaignEvent(campaign.Title, campaign.Description,
                    campaign.StartDate,campaign.EndDate,
                    campaign.FinancialGoal,campaign.Status),CancellationToken.None
            );

            
            
            //await _elasticClient.IndexAsync(campaign,CatalogIndexName);
            
            // Limpar cache da lista de Campanhas
            await _cacheService.RemoveAsync(CampaingListCacheKey);
            
            _logger.LogInformation($"Campanha {campaign.Title} (ID: {campaign.Id}) criado com sucesso pelo manager {userGuid}");
            
            return CreatedAtAction(nameof(Get), new { id = campaign.Id }, campaign);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao criar Campanha: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }

    private Status CheckStatusByDate(DateTime campaignInputStartDate)
    {
        return campaignInputStartDate < DateTime.Now ? Status.Active : Status.Scheduled;
    }

    [HttpPut]
    [Authorize(Policy = nameof(Role.Manager))]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Put([FromBody] CampaignUpdateInput updateInput)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            // Verificar se o usuário tem permissão de Admin
            // if (userRole != nameof(PermissionType.Admin))
            // {
            //     _logger.LogWarning($"Usuário {username} tentou atualizar jogo {gameInput.Id} sem permissão de Admin.");
            //     return Forbid("Acesso negado. Apenas administradores podem atualizar jogos.");
            // }

            _logger.LogInformation($"Manager {username} atualizando campanha ID: {updateInput.Id}");

            var campaign = _campaignRepository.GetById(updateInput.Id);
            
            if (campaign == null)
            {
                return NotFound(new { message = $"Campanha com ID {updateInput.Id} não encontrado." });
            }
            
            var validationResult = await _campaignUpdateValidator.ValidateAsync(updateInput);
            
            if(!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToString());
            }
            
            campaign.Title = updateInput.Title;
            campaign.Description = updateInput.Description;
            campaign.FinancialGoal = updateInput.FinancialGoal;
            campaign.StartDate = updateInput.StartDate;
            campaign.EndDate = updateInput.EndDate;
            campaign.Status = updateInput.Status;
            
            _campaignRepository.Update(campaign);
            
            
            
            await _rabbitMqService.PublishAsync(
                "campaign.events",
                "campaign.updated",
                new UpdateCampaignEvent(campaign.Id, campaign.Title, campaign.Description,
                    campaign.StartDate,campaign.EndDate,
                    campaign.FinancialGoal,campaign.Status),CancellationToken.None
            );
            
            
            //ElasticSearch
            //await _elasticClient.IndexAsync(campaign, CatalogIndexName);
            
            // Limpar cache relacionado
            await _cacheService.RemoveAsync(CampaingListCacheKey);
            await _cacheService.RemoveAsync($"game-{campaign.Id}");
            
            _logger.LogInformation($"Campanha {campaign.Title} (ID: {campaign.Id}) atualizado com sucesso pelo Manager {username}");
            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao atualizar Campanha {updateInput.Id}: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }
    
    /// <summary>
    /// Deleta um jogo pelo ID. Requer permissão de Admin.
    /// </summary>
    /// <param name="id">O ID do jogo a ser deletado.</param>
    /// <returns>Nenhum conteúdo em caso de sucesso.</returns>
    [HttpDelete("{id:Guid}")]
    [Authorize(Policy = nameof(Role.Manager))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            // Verificar se o usuário tem permissão de Admin
            // if (userRole != nameof(PermissionType.Admin))
            // {
            //     _logger.LogWarning($"Usuário {username} tentou deletar jogo {id} sem permissão de Admin.");
            //     return Forbid("Acesso negado. Apenas administradores podem deletar jogos.");
            // }

            _logger.LogInformation($"Usuário: {username} tentando deletar Campanha ID: {id}");

            var game = _campaignRepository.GetById(id);
            if (game == null)
            {
                return NotFound(new { message = $"Jogo com ID {id} não encontrado." });
            }
            
            _campaignRepository.Delete(id);
            
            //ElasticSearch
            //await _elasticClient.DeleteAsync(id,CatalogIndexName);
            
            // Limpar cache relacionado
            await _cacheService.RemoveAsync(CampaingListCacheKey);
            await _cacheService.RemoveAsync($"campaign-{id}");
            
            _logger.LogInformation($"Campanha {game.Title} (ID: {id}) deletado com sucesso pelo admin {username}");
            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao deletar jogo {id}: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Ocorreu um erro interno.", 
                error = e.Message 
            });
        }
    }
    

    // [HttpGet("search")]
    // public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? category = null)
    // {
    //     if (string.IsNullOrWhiteSpace(q))
    //         return BadRequest(new { error = "Parâmetro 'q' é obrigatório" });
    //     
    //     var results = await _elasticClient.SearchAsync(CatalogIndexName, q, category);
    //     
    //     return Ok(results);
    // }
    //
    // [HttpPost("reindex")]
    // public async Task<IActionResult> Reindex()
    // {
    //     try
    //     {
    //         var games = _campaignRepository.GetAll();
    //     
    //         foreach (var game in games)
    //         {
    //             await _elasticClient.IndexAsync(game, CatalogIndexName);
    //         }
    //     
    //         _logger.LogInformation($"{games.Count} jogos reindexados com sucesso.");
    //         return Ok(new { message = $"{games.Count} jogos reindexados com sucesso." });
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError($"Erro ao reindexar: {e.Message}");
    //         return StatusCode(500, new { message = "Erro ao reindexar.", error = e.Message });
    //     }
    // }

    /// <summary>
    /// Endpoint público para verificar status do serviço de catálogo.
    /// </summary>
    /// <returns>Status do serviço.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        _logger.LogInformation($"Health");
        return Ok(new 
        { 
            status = "healthy", 
            service = "CatalogAPI - Games", 
            timestamp = DateTime.UtcNow 
        });
    }
    
    
}