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
    
    [HttpPost]
    [Authorize(Policy = nameof(Role.Donor))]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DonationInput), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Donation([FromBody] DonationInput donationInput)
    {
        try
        {
            // Pegar o userId do TOKEN
            
            
            // var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            // var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var game = _donationRepository.GetById(donationInput.CampaignId);
            
            if (game == null)
            {
                return NotFound(new { message = $"Campanha com ID {donationInput.CampaignId} não encontrado." });
            }

            //TODO
            // await _rabbitMqService.PublishAsync(
            //     "order.events",
            //     "order.ordered",
            //     new OrderPlacedEvent(donationInput.UserId, "teste", "TesTT", donationInput.GameId, game.Price),CancellationToken.None
            // );
            
            
            _logger.LogInformation($"Jogo {game.Name} (ID: {game.Id}) Criado Ordem de compra por: {donationInput.UserId}");
            
            return CreatedAtAction(nameof(Get), new { id = game.Id }, donationInput);
        }
        catch (Exception e)
        {
            _logger.LogError($"Erro ao criar ordem de compra jogo: {e.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new 
            { 
                message = "Erro interno do servidor.",
                error = e.Message
            });
        }
    }

    
}