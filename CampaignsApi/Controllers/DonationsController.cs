using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core;
using Core.Dtos;
using Core.Entity;
using Core.Models;
using Core.Models.ElasticSearch;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampaignsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DonationsController : ControllerBase
{
    private readonly IDonationRepository _donationRepository;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DonationsController> _logger;
    private readonly IElasticClient<Donation> _elasticClient;
    private readonly IValidator<DonationInput> _donationInputValidator;

    public DonationsController(IDonationRepository donationRepository, ICacheService cacheService, ILogger<DonationsController> logger,
        IRabbitMqService rabbitMqService, IElasticClient<Donation> elasticClient, IValidator<DonationInput> donationInputValidator)
    {
        _donationRepository = donationRepository;
        _cacheService = cacheService;
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _elasticClient = elasticClient;
        _donationInputValidator = donationInputValidator;
    }
    
    
    // [HttpGet]
    // [Authorize(Policy = nameof(Role.Manager))]
    // [ProducesResponseType(typeof(IEnumerable<Donation>), StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(StatusCodes.Status403Forbidden)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<IActionResult> Get()
    // {
    //     try
    //     {
    //         var username = User.FindFirst(ClaimTypes.Name)?.Value;
    //         var username = User.FindFirst(ClaimTypes.Name)?.Value;
    //         _logger.LogInformation($"Usuário {username} acessando lista de Camapanhas.");
    //
    //         var cachedGameList = await _cacheService.GetAsync<List<Donation>>(CampaingListCacheKey);
    //
    //         if (cachedGameList != null)
    //         {
    //             return Ok(cachedGameList);
    //         }
    //         
    //         var campaigns = _campaignRepository.GetAll();
    //
    //         if (campaigns.Count > 0)
    //         {
    //             await _cacheService.SetAsync(CampaingListCacheKey, campaigns, TimeSpan.FromMinutes(15));
    //         } 
    //         
    //         _logger.LogInformation($"Retornados {campaigns.Count} campanhas {username}.");
    //         return Ok(campaigns);
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError($"Erro ao buscar lista de campanhas: {e.Message}");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new 
    //         { 
    //             message = "Ocorreu um erro interno ao buscar os jogos.",
    //             error = e.Message
    //         });
    //     }
    // }

    [HttpGet("{id:Guid}")]
    [Authorize(Policy = nameof(Role.Donor))]
    [ProducesResponseType(typeof(Donation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        try
        {
            var userGuid = ValidateUserToken(Role.Donor);
            
            if(userGuid == Guid.Empty)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                return Unauthorized($"user: {userGuid} userRole: {userRole}");
            }
            
            
            var donationKey = $"donation-{id}";
            
            var cachedGame = await _cacheService.GetAsync<Donation>(donationKey);
            
            if (cachedGame != null)
            {
                return Ok(cachedGame);
            }
    
            var donation = _donationRepository.GetById(id);
            
            if (donation == null)
            {
                return NotFound(new { message = $"Donation com ID {id} não encontrado." });
            }
            
            await _cacheService.SetAsync(donationKey, donation, TimeSpan.FromMinutes(15));
            
            _logger.LogInformation($"Donation {id} ");
            return Ok(donation);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao buscar donation {id}: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
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
    
    
    
    [HttpPost]
    [Authorize(Policy = nameof(Role.Donor))]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DonationInput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] DonationInput donationInput)
    {
        try
        {
            var userGuid = ValidateUserToken(Role.Donor);
            
            if(userGuid == Guid.Empty)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                return Unauthorized($"user: {userGuid} userRole: {userRole}");
            }

            var validateDonation = await _donationInputValidator.ValidateAsync(donationInput);

            if (!validateDonation.IsValid)
            {
                return BadRequest(validateDonation.ToDictionary());
            }

            var donation = new Donation()
            {
                CampaignId = donationInput.CampaignId,
                UserId = userGuid,
                Amount = donationInput.Amount,
                CreatedAt = DateTime.Now
            };
            
            _donationRepository.Add(donation);
            
            await _rabbitMqService.PublishAsync(
                "donation.events",
                "donation.received",
                new DonationReceivedEvent(donationInput.CampaignId, donationInput.Amount),CancellationToken.None
            );
            
            
            _logger.LogInformation($"Donation User: {userGuid} Campanha {donationInput.CampaignId} Amount: {donationInput.Amount}");
            
            return CreatedAtAction(nameof(Get), new { id = donation.Id }, donationInput);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao criar Donation: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }
}