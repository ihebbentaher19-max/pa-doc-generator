# Smoke tests hors-ligne (sans NuGet)

Ce petit projet console compile et exécute la logique métier "pure" (validation
JSON, parsing du flux Power Automate, mise en forme) **sans aucune dépendance
NuGet externe** — uniquement la bibliothèque de base .NET (`System.Text.Json`).

Objectif : pouvoir vérifier rapidement, y compris sur une machine/CI sans accès
à un flux NuGet, que la logique de parsing n'est pas cassée, avant même de
lancer la suite de tests complète `PADocGenerator.Tests` (qui nécessite xUnit,
Moq, EF Core, etc. via NuGet).

Les fichiers dans `Logic/` sont des **copies** des fichiers sources réels du
projet `PADocGenerator.Api` (`Services/FlowValidationService.cs`,
`Services/FlowParserService.cs`, `Services/DocumentFormattingService.cs`, ...).
Si vous modifiez la logique dans `PADocGenerator.Api`, pensez à répercuter le
changement ici (ou mieux : à terme, remplacez ces copies par un lien de projet
partagé une fois que l'environnement de build a accès à NuGet).

## Lancer les tests

```bash
cd backend/PADocGenerator.SmokeTests
dotnet run
```

Sortie attendue : `===== RESULTATS : 27 reussis / 0 echoues (total 27) =====`
