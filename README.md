# Générateur de documentation IA pour Power Automate

Plateforme permettant de sélectionner un flux Microsoft Power Automate auquel
l'utilisateur a accès dans son environnement Power Platform, ou d'importer un
flux exporté au format JSON, puis de générer automatiquement, via Azure OpenAI, une documentation
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
| `Services/PowerPlatformFlowService.cs`               | Connexion déléguée Microsoft 365 / Power Platform |
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

### Connexion Microsoft 365 / Power Platform (facultatif mais recommandé)

L'import direct ne requiert **aucun secret Microsoft stocké dans le projet**.
Il utilise OAuth 2.0 Authorization Code avec PKCE dans le navigateur ; les
jetons délégués sont gardés uniquement en mémoire le temps de la session et le
backend les transmet aux API Microsoft sans les enregistrer.

1. Dans Microsoft Entra ID, créez une inscription d'application de type
   **Single-page application (SPA)**, idéalement restreinte au tenant de
   l'organisation.
2. Ajoutez `http://localhost:5173` (et l'URL de production) aux URI de
   redirection SPA, puis accordez les permissions déléguées nécessaires au
   service Power Platform/Dataverse selon la politique du tenant. Le consentement
   administrateur peut être requis.
3. Copiez `frontend/.env.example` vers `frontend/.env` et renseignez au minimum
   `VITE_ENTRA_CLIENT_ID`; définissez `VITE_ENTRA_TENANT_ID` avec l'ID du tenant
   en production plutôt que `organizations`.
4. L'utilisateur ouvre **Importer un flux**, se connecte à Microsoft 365,
   choisit son environnement, puis son flux. Au moment de l'import, il autorise
   également l'accès délégué à la base Dataverse de l'environnement pour lire la
   définition du flux.

L'implémentation s'appuie sur l'API Power Platform documentée pour la liste des
[environnements accessibles](https://learn.microsoft.com/en-us/rest/api/power-platform/environmentmanagement/environments/list-environments-for-user)
et des [flux cloud](https://learn.microsoft.com/en-us/rest/api/power-platform/powerautomate/cloud-flows/list-cloud-flows), puis sur l'API Dataverse pour lire
`workflow.clientdata`, qui contient la définition et les références de connexion
du flux. Microsoft indique que la gestion programmatique des flux est centrée
sur les flux Dataverse/solutions : certains flux « Mes flux » ou environnements
sans Dataverse peuvent donc rester indisponibles et le JSON manuel reste prévu
comme solution de repli.

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
- [x] Import Microsoft 365 / Power Platform : OAuth PKCE, choix d'environnement et de flux, lecture déléguée de la définition Dataverse
- [ ] Configuration réelle Azure OpenAI + PostgreSQL (secrets à fournir par l'entreprise)
- [ ] Migration EF Core initiale (`dotnet ef migrations add InitialCreate`)
- [ ] Déploiement Azure DevOps (repo distant, pipeline branché sur un environnement réel)
- [ ] Revue de sécurité (rotation des secrets, politique de mot de passe, rate limiting)
