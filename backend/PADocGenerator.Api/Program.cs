using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposé pour les tests d'intégration (WebApplicationFactory<Program>).
public partial class Program { }
