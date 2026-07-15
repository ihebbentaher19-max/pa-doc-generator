# Générateur de documentation IA pour Power Automate

Plateforme permettant d'importer un flux Microsoft Power Automate exporté au
format JSON et de générer automatiquement, via Azure OpenAI, une documentation
claire, structurée et exploitable (résumé fonctionnel, description des étapes,
dépendances, étapes importantes), modifiable avant enregistrement, versionnée,
recherchable et exportable en PDF/Word.

Ce dépôt implémente le [cahier des charges](docs/cahier-des-charges.pdf) du
projet de stage, dans un seul dépôt Git regroupant `backend/` et `frontend/`
(conformément à la politique par défaut d'un dépôt unique par projet).

## Stack technique (cf. cahier des charges, section 5)

| Catégorie          | Choix retenu                                   |
|---------------------|-------------------------------------------------|
| Frontend             | React (Vite)                                    |
| Backend              | ASP.NET Core Web API (C# / .NET 8)              |
| Base de données      | PostgreSQL (JSONB pour les flux importés)       |
| IA générative        | Azure OpenAI                                    |
| Gestion de code      | Git + Azure Repos (Azure DevOps)                |
| CI/CD                | Azure DevOps Pipelines (`azure-pipelines.yml`)  |

## Structure du dépôt

```
pa-doc-generator/
├── backend/
│   ├── PADocGenerator.Api/          # API ASP.NET Core (Controllers, Services, Data, Models)
│   ├── PADocGenerator.Tests/        # Tests xUnit (nécessite NuGet)
│   └── PADocGenerator.SmokeTests/   # Tests de logique pure, compilables hors-ligne (voir plus bas)
├── frontend/
│   └── src/                         # Application React (pages, composants, services API)
├── docs/                            # Cahier des charges et documentation complémentaire
├── azure-pipelines.yml              # Pipeline CI/CD (build + test backend/frontend)
└── .azuredevops/                    # Réservé aux futurs artefacts Azure Boards / Repos
```

Chaque dossier de `backend/PADocGenerator.Api/` correspond à un module du
cahier des charges (section 6) :

| Dossier / fichier                                | Module (cahier des charges §6)             |
|----------------------------------------------------|---------------------------------------------|
| `Controllers/FlowsController.cs`                    | Module d'importation                        |
| `Services/FlowParserService.cs`                     | Module de lecture et préparation des données|
| `Services/PromptBuilderService.cs` + `AzureOpenAiDocumentationService.cs` | Module de génération de documentation |
| `Services/DocumentFormattingService.cs`             | Module de mise en forme                     |
| `Services/DocumentManagementService.cs`             | Module de gestion documentaire              |
| `Services/SearchService.cs`                         | Module de recherche et consultation         |
| `Services/ExportService.cs` (+ Pdf/Word renderers)  | Module d'export                             |
| `Controllers/UsersController.cs` + `AuthService.cs` | Module de gestion des rôles                 |
| `Controllers/DashboardController.cs`                | Module de tableau de bord                   |

## Démarrage local

### Prérequis
- .NET SDK 8.0
- Node.js 20+
- PostgreSQL 14+ (une base vide, ex. `padocgenerator`)
- Un déploiement Azure OpenAI (endpoint + clé + nom de déploiement)

### Backend

```bash
cd backend
dotnet restore
# Configurer backend/PADocGenerator.Api/appsettings.json (ou des variables
# d'environnement / dotnet user-secrets) avec :
#   - ConnectionStrings:DefaultConnection
#   - AzureOpenAI:Endpoint / ApiKey / DeploymentName
#   - Jwt:SigningKey (secret aléatoire, 32+ caractères)
dotnet ef database update --project PADocGenerator.Api   # nécessite dotnet-ef
dotnet run --project PADocGenerator.Api
# API disponible sur http://localhost:5080, Swagger sur /swagger
```

### Frontend

```bash
cd frontend
npm install
cp .env.example .env   # ajuster VITE_API_BASE_URL si besoin
npm run dev
```

### Tests

```bash
# Suite complète (nécessite l'accès à NuGet, ex. en local ou en CI)
cd backend
dotnet test PADocGenerator.Tests

# Tests de logique métier pure, sans aucune dépendance NuGet (voir
# backend/PADocGenerator.SmokeTests/README.md) : utile pour vérifier
# rapidement le parsing/la validation sur une machine sans accès NuGet.
cd backend/PADocGenerator.SmokeTests
dotnet run
```

## Workflow Git (consigne de l'encadrant)

> Par défaut, chaque projet dispose d'un seul dépôt pour le code source. La
> branche `main` représente toujours la version la plus stable du projet.
> Chaque personne crée une branche à son nom pour ses commits quotidiens,
> puis ouvre une Pull Request vers `main` une fois une fonctionnalité
> terminée, propre et testée.

En pratique :

```bash
# Une seule fois : cloner le dépôt et créer sa branche personnelle
git clone <url-du-depot-azure-repos>
cd pa-doc-generator
git checkout -b dev/<votre-nom>

# Au quotidien : committer et pousser son travail en fin de journée
git add .
git commit -m "Description claire du travail du jour"
git push origin dev/<votre-nom>

# Une fois une fonctionnalité terminée, propre et testée :
# ouvrir une Pull Request dev/<votre-nom> -> main sur Azure Repos
# (jamais de push direct sur main)
```

Recommandations complémentaires :
- Un commit par unité de travail cohérente (éviter les gros commits fourre-tout).
- Le message de commit décrit *quoi* et *pourquoi*, pas seulement *quoi*.
- Avant d'ouvrir une Pull Request : `dotnet build` + `dotnet test` côté
  backend, `npm run build` + `npm run lint` côté frontend doivent passer
  (le pipeline `azure-pipelines.yml` les revérifie automatiquement sur la PR).
- Ne jamais committer `appsettings.*.local.json`, `.env`, ou toute clé/secret
  (Azure OpenAI, chaîne de connexion) — cf. `.gitignore`.

## État d'avancement

- [x] Cahier des charges finalisé
- [x] Backend : entités, DTOs, tous les services/modules, contrôleurs, auth JWT + rôles
- [x] Backend : tests de logique métier (parsing/validation/mise en forme) validés
- [x] Frontend : authentification, tableau de bord, import, génération, consultation/édition, recherche, administration
- [ ] Configuration réelle Azure OpenAI + PostgreSQL (secrets à fournir par l'entreprise)
- [ ] Migration EF Core initiale (`dotnet ef migrations add InitialCreate`)
- [ ] Déploiement Azure DevOps (repo distant, pipeline branché sur un environnement réel)
- [ ] Revue de sécurité (rotation des secrets, politique de mot de passe, rate limiting)
