using Microsoft.EntityFrameworkCore;
using PADocGenerator.Api.Data;

namespace PADocGenerator.Tests;

/// <summary>
/// Fournit une instance fraîche d'<see cref="AppDbContext"/> adossée au
/// fournisseur EF Core InMemory, pour tester les services qui dépendent de
/// la base sans avoir besoin d'un vrai PostgreSQL. Chaque appel utilise un
/// nom de base unique (Guid) pour garantir l'isolation entre tests.
/// </summary>
internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
