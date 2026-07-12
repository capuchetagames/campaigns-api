using CampaignsApi.Config;
using CampaignsApi.Middlewares;
using CampaignsApi.Service;
using CampaignsApi.Service.DynamoLogging;
using CampaignsApi.Service.Extensions;
using CampaignsApi.Service.RedisCache;
using CampaignsApi.Service.Validator;
using CatalogApi.Service;
using Core.Models;
using Core.Models.ElasticSearch;
using Core.Repository;
using FluentValidation;
using Infrastructure.ElasticSearch;
using Infrastructure.Repository;
using Microsoft.Extensions.Options;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDynamoDb(builder.Configuration);

var logTableName    = builder.Configuration["DynamoDb:LogTableName"];

builder.Logging
    .ClearProviders()                      
    .AddConsole()                          
    .AddDynamoDbLogger(logTableName, LogLevel.Warning);

builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<CampaignInputValidator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient<ICorrelationIdService, CorrelationIdService>();
builder.Services.AddScoped(typeof(IBaseLogger<>), typeof(BaseLogger<>));


//Config de cache com Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "catalog:"; // prefixo nas chaves
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();


// Registrar repositórios
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<IDonationRepository, DonationRepository>();


// Configuração do HttpClient para comunicação com UserAPI
builder.Services.AddHttpClient("AccountApi", client =>
{
    var accountApiUrl = builder.Configuration["Services:AccountApi:BaseUrl"] ?? "http://account-api:8080/";
    client.BaseAddress = new Uri(accountApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Registrar serviços de validação de token
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

// Autenticação JWT (mesma chave/emissor da AccountsApi) e policies por Role
builder.AddJwtAuthentication();
builder.Services.AddPolicyAuthorization();

// ElasticSearch (client é instanciado de forma lazy; UseCloud=false usa apenas a LocalUrl)
builder.Services.AddSingleton<IElasticSettings>(
    builder.Configuration.GetSection("ElasticSettings").Get<ElasticSettings>() ?? new ElasticSettings
    {
        UseCloud = false,
        LocalUrl = "http://localhost:9200",
        ApiKey = string.Empty,
        CloudId = string.Empty
    });
builder.Services.AddSingleton(typeof(IElasticClient<>), typeof(ElasticClient<>));

builder.Services.AddHealthChecks();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<IRabbitMqService>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
    
    return RabbitMqService.CreateAsync(settings, logger).GetAwaiter().GetResult();
});

//TODO
//builder.Services.AddHostedService<PaymentProcessConsumer>();

var app = builder.Build();

app.UseLogMiddleware();
app.UseDynamoLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
    
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.UseReDoc(c =>
    {
        c.DocumentTitle = "REDOC API Documentation";
        c.SpecUrl = "/swagger/v1/swagger.json";
    });
}


app.UseHttpMetrics();

app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


Console.WriteLine("Campaigns API Up and Running!");

app.Run();