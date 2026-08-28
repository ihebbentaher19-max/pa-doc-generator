using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PADocGenerator.Api.Data;
using PADocGenerator.Api.Middleware;
using PADocGenerator.Api.Services;
using PADocGenerator.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Base de données - PostgreSQL (choix retenu, cahier des charges section 5)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------------------
// Azure OpenAI (module de génération)
// ---------------------------------------------------------------------------
builder.Services.Configure<AzureOpenAiOptions>(builder.Configuration.GetSection(AzureOpenAiOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<MicrosoftEntraOptions>(builder.Configuration.GetSection("MicrosoftEntra"));

// ---------------------------------------------------------------------------
// Injection de dépendances - un service par module du cahier des charges (§6)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IFlowValidationService, FlowValidationService>();       // Module d'importation
builder.Services.AddScoped<IFlowParserService, FlowParserService>();               // Module de lecture et préparation
builder.Services.AddSingleton<PromptBuilderService>();                             // Module de génération (prompt)
builder.Services.AddScoped<IAiDocumentationService, AzureOpenAiDocumentationService>(); // Module de génération (IA)
builder.Services.AddScoped<IDocumentFormattingService, DocumentFormattingService>(); // Module de mise en forme
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>(); // Module de gestion documentaire
builder.Services.AddScoped<ISearchService, SearchService>();                       // Module de recherche et consultation
builder.Services.AddSingleton<PdfDocumentationRenderer>();                         // Module d'export (PDF)
builder.Services.AddSingleton<WordDocumentationRenderer>();                        // Module d'export (Word)
builder.Services.AddScoped<IExportService, ExportService>();                       // Module d'export (façade)
builder.Services.AddScoped<IAuthService, AuthService>();                           // Module de gestion des rôles (auth)
builder.Services.AddScoped<IDashboardService, DashboardService>();                 // Module de tableau de bord
builder.Services.AddHttpClient<IPowerPlatformFlowService, PowerPlatformFlowService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
}); // Import délégué depuis Microsoft 365 / Power Platform
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// ---------------------------------------------------------------------------
// Authentification JWT + rôles (administrateur / utilisateur)
// ---------------------------------------------------------------------------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtSigningKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey manquant dans la configuration.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// CORS - autorise le frontend React (séparé, sur un autre port/domaine)
// ---------------------------------------------------------------------------
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---------------------------------------------------------------------------
// Contrôleurs + Swagger/OpenAPI (justifié section 5 : génération automatique
// de la documentation Swagger/OpenAPI, un des avantages d'ASP.NET Core)
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Générateur de documentation IA pour Power Automate - API",
        Version = "v1",
        Description = "API REST du projet de stage : import de flux Power Automate, " +
                      "génération de documentation via Azure OpenAI, gestion documentaire, export."
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Entrez uniquement le jeton JWT (sans le préfixe 'Bearer').",
        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
    };

    options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtSecurityScheme, Array.Empty<string>() } });
});

var configuredUrls = builder.Configuration["ASPNETCORE_URLS"];
if (string.IsNullOrWhiteSpace(configuredUrls))
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }
    else
    {
        builder.WebHost.UseUrls("http://localhost:5080");
    }
}
else
{
    builder.WebHost.UseUrls(configuredUrls);
}

var app = builder.Build();

// 1. Proxys et CORS en tout premier pour répondre immédiatement aux requêtes Preflight
app.UseForwardedHeaders();
app.UseCors(FrontendCorsPolicy); // <-- Déplacé ici

// 2. Gestion des erreurs et documentation
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Générateur de documentation IA pour Power Automate - API");
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposé pour les tests d'intégration (WebApplicationFactory<Program>).
public partial class Program { }
