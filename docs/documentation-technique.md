# Documentation technique — Générateur de documentation IA pour Power Automate

Ce document explique (1) la démarche et l'ordre de création du projet, (2) les
dépendances entre fichiers, (3) le détail de chaque fichier, et (4) une
vérification de conformité au cahier des charges avec les points restants à
traiter. Il complète le `README.md` (orienté « démarrage rapide ») par une vue
exhaustive, utile pour un rapport de stage ou une revue technique.

---

## 1. Démarche de création et dépendances entre fichiers

### 1.1 Principe général : construire du bas vers le haut

Le projet a été construit en respectant l'ordre naturel des dépendances : on
ne peut pas écrire un service qui utilise une entité avant que cette entité
n'existe. L'ordre réel de création (visible dans l'historique Git, 110+
commits sur `main`) suit six paliers pour le backend et cinq pour le
frontend — chaque palier ne dépend que du(des) palier(s) immédiatement en
dessous de lui :

**Backend (C# / ASP.NET Core)**
```
Program.cs                              (assemble tout : DI, JWT, CORS, Swagger)
      ↓ dépend de
Controllers + Middleware                (7 endpoints REST, gestion d'erreurs)
      ↓ dépend de
Services (12 fichiers)                  (implémentent les interfaces)
      ↓ dépend de
Interfaces (9 fichiers)                 (contrats par module métier)
      ↓ dépend de
AppDbContext                            (EF Core, mapping PostgreSQL)
      ↓ dépend de
Entités, DTOs, ParsedFlow               (aucune dépendance interne)
```

**Frontend (React)**
```
App.jsx + main.jsx                      (routing, point d'entrée)
      ↓ dépend de
Pages (7 fichiers)                      (un écran fonctionnel chacun)
      ↓ dépend de
Layout + composants UI                  (Sidebar, PipelineTrail, Button...)
      ↓ dépend de
AuthContext + useAuth                   (état utilisateur, JWT en mémoire)
      ↓ dépend de
Services API (6 fichiers)               (client axios, aucune dépendance interne)
```

### 1.2 Pourquoi cet ordre précisément

1. **Entités avant tout** — elles sont le vocabulaire commun du projet (`FlowImport`, `Documentation`, `DocumentationVersion`, `ApplicationUser`) et n'importent rien d'autre que le C# de base.
2. **DTOs et `ParsedFlow` juste après** — ce sont des structures de données pures (records C#), nécessaires pour typer les interfaces qui arrivent ensuite.
3. **`AppDbContext` seulement après les entités** — il a besoin de connaître `FlowImport`, `Documentation`, etc. pour définir les `DbSet<T>` et le mapping EF Core → PostgreSQL (colonnes `jsonb`).
4. **Les 9 interfaces avant les implémentations** — chaque interface (`IFlowValidationService`, `IFlowParserService`, ...) correspond à un module du cahier des charges (section 6) et fixe le contrat *avant* d'écrire le code métier, pour que les Controllers puissent être écrits en parallèle sans attendre l'implémentation finale (principe d'inversion de dépendances).
5. **Les services dans l'ordre du pipeline métier** — validation → parsing → prompt/IA → mise en forme, parce que chaque étape sert de donnée d'entrée à la suivante et qu'il est plus facile de tester une étape une fois la précédente stabilisée. C'est à ce stade que le harnais `PADocGenerator.SmokeTests` a été créé (voir §1.3), pour valider réellement le parsing sans attendre l'accès à NuGet.
6. **Les controllers en dernier côté API** — ils ne font qu'orchestrer les services via injection de dépendances ; les écrire tôt sans les services aurait juste produit du code qui ne compile pas.
7. **`Program.cs` tout à la fin du backend** — c'est le point de câblage final : il ne peut être écrit correctement qu'une fois qu'on connaît la liste complète des interfaces/implémentations à enregistrer dans le conteneur DI.
8. **Le frontend suit la même logique, inversée dans le sens de lecture** — les services API (qui parlent au backend) sont écrits en premier car ils ne dépendent de rien d'interne ; viennent ensuite le contexte d'authentification (qui utilise ces services), puis les composants UI génériques (qui ne dépendent de rien de métier), puis les pages (qui assemblent services + contexte + composants), puis enfin le routing (`App.jsx`) qui assemble toutes les pages.

### 1.3 Une branche parallèle : le harnais de tests hors-ligne

Après avoir écrit `FlowValidationService`, `FlowParserService`,
`PromptBuilderService` et `AzureOpenAiDocumentationService`, un projet
séparé `PADocGenerator.SmokeTests` a été créé **spécifiquement parce que ce
sandbox de développement n'a pas accès à NuGet** (donc pas moyen de
compiler le vrai projet `PADocGenerator.Api`, qui dépend d'EF Core, Azure
OpenAI, etc.). Ce harnais copie uniquement la logique qui n'a besoin que de
la bibliothèque standard .NET (`System.Text.Json`), et a permis d'exécuter
de vrais tests. C'est ce harnais qui a révélé le bug de parsing des
variables `InitializeVariable` (voir commit `fix(parser)` dans l'historique
Git) — un vrai exemple de cycle écrire → tester → corriger, conservé tel
quel dans l'historique plutôt que lissé.

### 1.4 Dépendances externes (NuGet / npm) par fichier

| Fichier | Dépend de (package externe) |
|---|---|
| `AppDbContext.cs` | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `AzureOpenAiDocumentationService.cs` | `Azure.AI.OpenAI`, `Azure.Identity` |
| `AuthService.cs` | `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens` (transitifs via `Microsoft.AspNetCore.Authentication.JwtBearer`) |
| `PdfDocumentationRenderer.cs` | `QuestPDF` |
| `WordDocumentationRenderer.cs` | `DocumentFormat.OpenXml` |
| `Program.cs` | `Swashbuckle.AspNetCore` (Swagger), tous les packages ci-dessus |
| `frontend/src/services/*.js` | `axios` |
| `frontend/src/App.jsx` | `react-router-dom` |
| `frontend/src/components/**` | `lucide-react` (icônes) |

Tous les autres fichiers backend (entités, DTOs, interfaces, la majorité des
services, tous les controllers) **ne dépendent d'aucun package externe** au-delà
du SDK ASP.NET Core lui-même — c'est volontaire, pour limiter la surface de
risque de compilation.

---

## 2. Détail de chaque fichier

### 2.1 Backend — Squelette de solution

- **`PADocGenerator.sln`** — fichier solution Visual Studio/`dotnet`, référence les 3 projets (`Api`, `Tests`, `SmokeTests`). Permet `dotnet build`/`dotnet test` sur l'ensemble en une commande.
- **`PADocGenerator.Api/PADocGenerator.Api.csproj`** — projet Web API .NET 8. Déclare tous les `PackageReference` nécessaires (EF Core/Npgsql, Azure.AI.OpenAI, JWT Bearer, QuestPDF, OpenXml, Swashbuckle, FluentValidation, AutoMapper).
- **`PADocGenerator.Tests/PADocGenerator.Tests.csproj`** — projet de tests xUnit, référence `PADocGenerator.Api` en `ProjectReference` + xUnit/Moq/FluentAssertions.

### 2.2 Backend — Entités (`Models/Entities/`)

- **`UserRole.cs`** — `enum` à deux valeurs (`Utilisateur`, `Administrateur`), correspond exactement à l'exigence de la section 4 du cahier des charges.
- **`DocumentationStatus.cs`** — `enum` à trois valeurs (`Brouillon`, `Valide`, `Archive`), noms d'identifiants C# sans accents mais correspondant terme à terme au texte du cahier des charges (« brouillon, validé, archivé »).
- **`ApplicationUser.cs`** — utilisateur de la plateforme : `Id`, `FullName`, `Email`, `PasswordHash`, `Role`, `IsActive`, `CreatedAtUtc`, et une collection `ImportedFlows`.
- **`FlowImport.cs`** — représente un flux Power Automate importé : `RawJson` (le JSON brut, stocké en colonne `jsonb`), `Name`, `ActionsCount`, `IsValid`/`ValidationError` (résultat de la validation), métadonnées d'import.
- **`Documentation.cs`** — une documentation générée pour un flux : `Title`, `Status`, `CurrentVersionNumber`, et une collection `Versions`.
- **`DocumentationVersion.cs`** — une version figée du contenu (`FunctionalSummary`, `StructuredContentJson` en `jsonb`, `IsManuallyEdited`, `ChangeNote`) — c'est cette entité qui porte tout l'historique de versions exigé par le cahier des charges.

### 2.3 Backend — Modèle intermédiaire (`Models/FlowSchema/ParsedFlow.cs`)

Contient cinq classes : `ParsedFlow` (racine : nom, déclencheur, listes),
`ParsedAction` (nom, type, connecteur, dépendance `RunsAfter`),
`ParsedCondition` (expression + branches vrai/faux), `ParsedVariable`
(nom/type/valeur initiale), `ParsedConnector` (nom/type). C'est la sortie du
module de lecture/préparation, l'entrée du module de génération — le fichier
« pivot » entre les deux.

### 2.4 Backend — DTOs (`Models/Dtos/`)

- **`AuthDtos.cs`** — `LoginRequestDto`, `LoginResponseDto`, `RegisterUserDto`.
- **`FlowImportDtos.cs`** — `FlowImportRequestDto`, `FlowImportResultDto`.
- **`DocumentationDtos.cs`** — le plus gros fichier de DTOs : `DocumentationContentDto` (le cœur — résumé, étapes, dépendances, étapes importantes), `DocumentationStepDto`, `DocumentationDependencyDto`, `DocumentationSummaryDto`/`DocumentationDetailDto` (listes vs détail), `UpdateDocumentationDto`, `ChangeStatusDto`, `DocumentationVersionSummaryDto`, `SearchDocumentationQueryDto`, `DashboardStatsDto`.

Tous sont des `record` C# (immutables, comparaison par valeur) — choix
délibéré pour des objets de transfert qui ne doivent jamais être mutés après
construction.

### 2.5 Backend — Accès aux données (`Data/AppDbContext.cs`)

Déclare les 4 `DbSet<T>` (`Users`, `FlowImports`, `Documentations`,
`DocumentationVersions`) et configure, dans `OnModelCreating` : les index
uniques (email utilisateur), les longueurs de colonnes, les conversions
`enum → string` (lisibilité en base), les colonnes `jsonb` pour `RawJson`
et `StructuredContentJson`, et toutes les relations (`FlowImport 1—N
Documentation 1—N DocumentationVersion`) avec leur comportement de
suppression (`Cascade` pour les versions, `Restrict` pour les utilisateurs
afin de ne jamais perdre la traçabilité de qui a créé/modifié quoi).

### 2.6 Backend — Interfaces (`Services/Interfaces/`, 9 fichiers)

Chacune correspond à un module de la section 6 du cahier des charges et ne
contient qu'une signature de méthode (parfois deux) :
`IFlowValidationService` (import), `IFlowParserService`
(lecture/préparation), `IAiDocumentationService` (génération),
`IDocumentFormattingService` (mise en forme), `IDocumentManagementService`
(gestion documentaire — 6 méthodes : create/get/update/changeStatus/
getVersions/delete), `ISearchService` (recherche), `IExportService`
(export, avec l'`enum ExportFormat`), `IAuthService` (rôles/auth),
`IDashboardService` (tableau de bord).

### 2.7 Backend — Services, implémentations (`Services/`, 12 fichiers)

- **`FlowValidationService.cs`** — parse le JSON avec `System.Text.Json`, vérifie la présence de `actions`/`triggers` (avec ou sans wrapper `properties.definition`). Ne fait aucune requête réseau, entièrement synchrone.
- **`FlowParserService.cs`** — le plus complexe algorithmiquement : parcourt récursivement les actions, détecte les `If` (→ `ParsedCondition` avec récursion dans les deux branches), détecte les variables (`InitializeVariable`, avec le vrai schéma `inputs.variables[]` depuis le fix), déduit les connecteurs depuis `inputs.host.connectionName`. **27 tests réels passent dessus** (voir `SmokeTests`).
- **`PromptBuilderService.cs`** — deux méthodes : `BuildSystemPrompt()` (instructions fixes + schéma JSON de sortie imposé au modèle) et `BuildUserPrompt(ParsedFlow)` (sérialise le flux en texte lisible pour l'IA). Aucune dépendance externe — testable isolément.
- **`AzureOpenAiDocumentationService.cs`** — construit un `AzureOpenAIClient`, appelle `CompleteChatAsync` avec `ResponseFormat` forcé en JSON, puis parse manuellement la réponse (`ParseModelResponse`) en `DocumentationContentDto`. Contient aussi `AzureOpenAiOptions` (classe de configuration liée à `appsettings.json:AzureOpenAI`).
- **`DocumentFormattingService.cs`** — pure LINQ : trim des textes, tri des étapes importantes en tête, `GroupBy` pour dédupliquer les dépendances et les étapes importantes.
- **`DocumentManagementService.cs`** — le service le plus riche fonctionnellement : `CreateFromGenerationAsync` (crée `Documentation` + première `DocumentationVersion`), `UpdateAsync` (crée une **nouvelle** version à chaque modification, jamais d'écrasement), `ChangeStatusAsync`, `GetVersionHistoryAsync`, `DeleteAsync`. Sérialise/désérialise `DocumentationContentDto` en JSON dans `StructuredContentJson`.
- **`SearchService.cs`** — requête EF Core avec `EF.Functions.ILike` (recherche insensible à la casse, spécifique PostgreSQL) sur le titre et le nom du flux, filtre optionnel par statut, pagination (`Skip`/`Take`).
- **`PdfDocumentationRenderer.cs`** — utilise QuestPDF (API fluide) pour composer un PDF : en-tête, résumé, tableau des étapes, liste des dépendances, étapes importantes, pied de page avec numérotation.
- **`WordDocumentationRenderer.cs`** — utilise `DocumentFormat.OpenXml` pour générer un `.docx` en mémoire (`MemoryStream`) : titres formatés manuellement (gras + taille de police, sans dépendre de styles Word non définis — voir le fix dans l'historique Git), tableau d'étapes, puces textuelles simples (`•`) plutôt qu'une numérotation Word non déclarée.
- **`ExportService.cs`** — façade : reçoit un `ExportFormat` (`Pdf`/`Word`) et délègue au renderer correspondant, calcule le nom de fichier (en filtrant les caractères invalides) et le `Content-Type`.
- **`AuthService.cs`** — `LoginAsync`/`RegisterAsync`, hachage de mot de passe en PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`, 100 000 itérations, comparaison en temps constant via `CryptographicOperations.FixedTimeEquals`), génération de JWT avec les claims `sub`/`email`/`name`/`role`. Contient aussi `JwtOptions` (config liée à `appsettings.json:Jwt`). Le tout premier compte créé devient automatiquement `Administrateur`.
- **`DashboardService.cs`** — agrège des `Count()` (par statut, total flux/documentations) et les 10 dernières documentations triées par date de mise à jour.

### 2.8 Backend — Controllers (`Controllers/`, 6 fichiers + 1 commun)

- **`AuthController.cs`** — `POST /api/auth/register`, `POST /api/auth/login`. Aucune protection `[Authorize]` (accès public, logique).
- **`Common/ClaimsPrincipalExtensions.cs`** — méthode d'extension `GetUserId()` qui lit le claim `sub` du JWT courant ; utilisée par tous les controllers protégés pour savoir « qui fait la requête ».
- **`FlowsController.cs`** — `POST /api/flows/import`, `GET /api/flows/{id}`. Enchaîne validation + parsing + enregistrement en base en une seule requête.
- **`DocumentationController.cs`** — le plus gros controller : `POST /generate` (orchestre parsing → IA → mise en forme → enregistrement), `GET/PUT /{id}`, `PATCH /{id}/status`, `GET /{id}/versions`, `GET /search`, `DELETE /{id}` (`[Authorize(Roles = "Administrateur")]` uniquement sur la suppression).
- **`ExportController.cs`** — `GET /api/documentation/{id}/export/pdf` et `.../word`, renvoie un `FileResult` (flux binaire téléchargeable).
- **`DashboardController.cs`** — `GET /api/dashboard`, un seul endpoint.
- **`UsersController.cs`** — tout le controller est `[Authorize(Roles = "Administrateur")]` : `GET /api/users`, `PATCH /{id}/role`, `PATCH /{id}/active`.

### 2.9 Backend — Middleware (`Middleware/ExceptionHandlingMiddleware.cs`)

Intercepte toute exception non gérée, la logge, et la convertit en réponse
JSON avec le bon code HTTP selon le type d'exception
(`KeyNotFoundException` → 404, `UnauthorizedAccessException` → 401,
`ArgumentException` → 400, `InvalidOperationException` → 422, sinon 500 avec
un message générique pour ne jamais exposer de détails internes au client).

### 2.10 Backend — Point d'entrée et configuration

- **`Program.cs`** — câble tout : `AddDbContext` (PostgreSQL), `Configure<AzureOpenAiOptions>`/`Configure<JwtOptions>`, `AddScoped`/`AddSingleton` pour les 9 interfaces + leurs implémentations, authentification JWT Bearer, CORS (origine configurable), Swagger avec schéma de sécurité Bearer, puis le pipeline HTTP (`ExceptionHandlingMiddleware` → Swagger (dev) → HTTPS → CORS → Auth → Controllers).
- **`appsettings.json`** — chaîne de connexion PostgreSQL, config Azure OpenAI, clé de signature JWT, origines CORS autorisées — tous en placeholders `CHANGE_ME` à remplacer.
- **`appsettings.Development.json`** — override : logs plus verbeux en développement.
- **`Properties/launchSettings.json`** — profils `http`/`https`, ports 5080/5081, ouverture automatique sur `/swagger`.

### 2.11 Backend — Tests

- **`PADocGenerator.Tests/SampleFlows.cs`** — fixture JSON partagée (flux « Approval Flow » réaliste, avec trigger/actions/condition/variable/connecteurs).
- **`FlowValidationServiceTests.cs`** — 6 tests (vide, JSON malformé, JSON valide sans actions/triggers, flux réaliste, flux sans wrapper `properties`).
- **`FlowParserServiceTests.cs`** — 9 tests (nom/déclencheur, actions, conditions avec branches, variables au bon schéma, connecteurs distincts, absence de dépendance sur la première action, action HTTP sans connecteur, flux sans wrapper).
- **`DocumentFormattingServiceTests.cs`** — 4 tests (trim, tri des étapes importantes, dédoublonnage des dépendances et des étapes importantes).
- **`PADocGenerator.SmokeTests/`** — projet console autonome (27 tests, exécutés réellement dans ce sandbox, voir §1.3) : `Program.cs` (les tests eux-mêmes) + `Logic/` (copies des fichiers source purs) + `NuGet.Config` (désactive les sources distantes pour compiler hors-ligne) + `README.md` (explique pourquoi ce projet existe).

### 2.12 Frontend — Scaffold et configuration

- **`package.json`** — généré par `create-vite`, dépendances ajoutées ensuite : `react-router-dom`, `axios`, `recharts`, `lucide-react`, `@fontsource/*`.
- **`vite.config.js`** — config Vite par défaut (plugin React).
- **`index.html`** — point de montage `#root`, titre mis à jour (« Générateur de documentation IA pour Power Automate »).
- **`.env.example`** — `VITE_API_BASE_URL`, à copier en `.env` pour pointer vers l'API locale ou déployée.
- **`.gitignore`** / **`.oxlintrc.json`** — générés par `create-vite` (le linter utilisé est `oxlint`, pas ESLint).

### 2.13 Frontend — Styles (`src/styles/`)

- **`tokens.css`** — toutes les variables CSS (`--color-*`, `--font-*`, `--space-*`, `--radius-*`) : palette indigo/ambre pensée pour un outil interne d'entreprise (voir justification donnée lors de la conception).
- **`global.css`** — reset, import des polices (Sora/Inter/JetBrains Mono), classes utilitaires (`.card`, `.stack`, `.row`, `.app-shell`), accessibilité (`:focus-visible`, `prefers-reduced-motion`).
- **`formStyles.js`** — objet `inputStyle` partagé par tous les formulaires (extrait dans son propre fichier pour respecter la règle de linter « fast refresh »).

### 2.14 Frontend — Services API (`src/services/`)

- **`api.js`** — instance `axios` centrale : intercepteur de requête (ajoute `Authorization: Bearer <token>`), intercepteur de réponse (déconnexion automatique sur 401), `getApiErrorMessage()` (extrait un message lisible d'une erreur Axios).
- **`authService.js`**, **`flowsService.js`**, **`documentationService.js`** (le plus gros : génération, CRUD, recherche, export avec téléchargement de blob), **`dashboardService.js`**, **`usersService.js`** — un fichier par domaine métier, chacun n'exportant que des fonctions `async` fines qui appellent `apiClient`.

### 2.15 Frontend — Contexte d'authentification (`src/context/`)

- **`AuthContext.jsx`** — `AuthProvider` : décode le JWT côté client (juste pour affichage, aucune validation cryptographique côté frontend — la vraie validation est côté serveur), expose `user`, `isAuthenticated`, `isAdmin`, `login`/`register`/`logout`.
- **`useAuth.js`** — hook consommateur, extrait dans son propre fichier (même raison que `formStyles.js`).

### 2.16 Frontend — Composants UI réutilisables (`src/components/ui/`)

`StatusBadge.jsx` (pastille colorée par statut), `Button.jsx` (4 variantes :
primary/secondary/ghost/danger), `EmptyState.jsx` (état vide générique),
`Spinner.jsx` (indicateur de chargement SVG animé), `Callout.jsx` (message
info/succès/erreur), `PageHeader.jsx` (en-tête de page standard), et
**`PipelineTrail.jsx`** — le composant « signature » de l'interface : un
stepper qui matérialise *exactement* le pipeline du cahier des charges
(Import → Lecture & préparation → Génération IA → Mise en forme →
Enregistrement), affiché en temps réel pendant la génération.

### 2.17 Frontend — Layout (`src/components/layout/`)

- **`Sidebar.jsx`** — navigation latérale, lien « Administration » affiché uniquement si `isAdmin`.
- **`AppLayout.jsx`** — exporte `ProtectedRoute` (redirige vers `/connexion` si non authentifié, vers `/` si route admin-only et rôle insuffisant) qui enveloppe la `Sidebar` + `<Outlet />`.

### 2.18 Frontend — Pages (`src/pages/`, 7 fichiers)

- **`LoginPage.jsx`** / **`RegisterPage.jsx`** — formulaires de connexion/inscription, gestion d'erreur via `Callout`.
- **`DashboardPage.jsx`** — cartes de statistiques + liste des dernières activités (module tableau de bord).
- **`ImportFlowPage.jsx`** — zone de dépôt de fichier (drag & drop + sélection), appelle `importFlow` puis, si valide, `generateDocumentation` avec animation du `PipelineTrail` pendant l'attente.
- **`DocumentationListPage.jsx`** — recherche avec debounce + filtre par statut (module recherche/consultation).
- **`DocumentationDetailPage.jsx`** — la page la plus complexe : édition du titre/résumé/étapes (ajout/suppression/marquage important), changement de statut, export PDF/Word, consultation de l'historique de versions.
- **`AdminUsersPage.jsx`** — tableau des utilisateurs, bascule de rôle et d'activation (module gestion des rôles, volet administration).

### 2.19 Frontend — Routing (`src/App.jsx`, `src/main.jsx`)

`App.jsx` déclare toutes les routes avec `react-router-dom` : routes
publiques (`/connexion`, `/inscription`), routes protégées (`/`,
`/importer`, `/documentations`, `/documentations/:id`), route admin-only
(`/administration`). `main.jsx` est le point d'entrée React (`createRoot`)
qui importe les styles globaux.

### 2.20 Racine du dépôt

- **`.gitignore`** — exclut `bin/`/`obj/` (.NET), `node_modules/`/`dist/` (frontend), tout fichier de secrets.
- **`azure-pipelines.yml`** — deux stages parallèles (Backend : restore/build/test ; Frontend : install/lint/build), déclenché sur `main` et sur toute Pull Request.
- **`README.md`** — vue d'ensemble, stack technique, démarrage local, workflow Git de l'encadrant.
- **`docs/cahier-des-charges.pdf`** — copie du cahier des charges original.
- **`docs/documentation-technique.md`** — ce document.

---

## 3. Vérification de conformité au cahier des charges

### 3.1 Fonctionnalités (section 4) — 13/13 couvertes

| # | Fonctionnalité du cahier des charges | Implémentation |
|---|---|---|
| 1 | Import JSON | `FlowsController` + `FlowValidationService` |
| 2 | Lancement de la génération depuis l'interface | `ImportFlowPage` → `POST /documentation/generate` |
| 3 | Affichage de la documentation générée | `DocumentationDetailPage` |
| 4 | Résumé fonctionnel en langage naturel | `PromptBuilderService` + `AzureOpenAiDocumentationService` |
| 5 | Description de chaque étape | idem |
| 6 | Explication des dépendances | idem (`DocumentationDependencyDto`) |
| 7 | Identification des étapes importantes | idem + tri en tête par `DocumentFormattingService` |
| 8 | Modification avant enregistrement | `DocumentationDetailPage` (édition) + `UpdateAsync` |
| 9 | Historique de versions | `DocumentationVersion` (nouvelle version à chaque modif) |
| 10 | Recherche et filtrage | `SearchService` + `DocumentationListPage` |
| 11 | Export PDF et Word | `PdfDocumentationRenderer` + `WordDocumentationRenderer` |
| 12 | Tableau de bord | `DashboardService` + `DashboardPage` |
| 13 | Rôles administrateur/utilisateur | `AuthService` + `UsersController` + `[Authorize(Roles=...)]` |

**Rien à ajouter fonctionnellement** — la couverture est complète au niveau du code. Ce qui reste, c'est la *preuve d'exécution* (voir §3.4).

### 3.2 Technologies (section 5) — conforme

React, ASP.NET Core Web API (.NET 8), **PostgreSQL avec JSONB** (bien la
version finale du cahier, pas Cosmos DB), Azure OpenAI, Git + Azure Repos,
Azure DevOps Pipelines. Rien à ajouter ici — tous les choix du tableau
comparatif de la section 5 sont respectés dans le code.

### 3.3 Découpage technique (section 6) — 9/9 modules mappés

Chaque module a un ou plusieurs fichiers dédiés et identifiables sans
ambiguïté (voir tableau dans le `README.md`). Le module de gestion des
rôles va même un peu plus loin que le texte du cahier des charges en
ajoutant un volet « administration » complet (`UsersController` +
`AdminUsersPage`) permettant de changer les rôles et désactiver des
comptes — une extension raisonnable, pas une dérive.

### 3.4 Ce qu'il faut ajouter pour passer de « conforme sur le papier » à « conforme en conditions réelles »

Rien ne manque dans le **découpage** ; ce qui manque, c'est la **preuve
d'exécution**, en 5 points classés par priorité :

1. **Compiler réellement le backend** (`dotnet restore && dotnet build` sur `backend/PADocGenerator.sln`) dans un environnement avec accès NuGet — jamais fait dans ce sandbox. Risque principal : les signatures exactes de l'API fluide QuestPDF et d'OpenXml, écrites de mémoire.
2. **Créer la migration EF Core initiale** (`dotnet ef migrations add InitialCreate`) — aucune base de données n'existe encore nulle part, la structure SQL n'a jamais été générée ni exécutée.
3. **Connecter de vrais services externes** : une instance PostgreSQL réelle, un déploiement Azure OpenAI réel (endpoint/clé/nom de déploiement), un vrai secret JWT — tout est actuellement `CHANGE_ME` dans `appsettings.json`.
4. **Compléter les tests unitaires manquants** : `DocumentManagementService`, `SearchService`, `DashboardService`, `AuthService` dépendent d'EF Core et n'ont pas encore de tests (ajouter `Microsoft.EntityFrameworkCore.InMemory` au projet `PADocGenerator.Tests` permettrait de les tester sans vraie base).
5. **Faire tourner le pipeline CI/CD sur un vrai agent Azure DevOps** — `azure-pipelines.yml` existe mais n'a jamais été exécuté hors de sa relecture manuelle.

Aucun de ces 5 points ne remet en cause l'architecture ou les choix de
conception ; ce sont des étapes de mise en service, pas de redéveloppement.
