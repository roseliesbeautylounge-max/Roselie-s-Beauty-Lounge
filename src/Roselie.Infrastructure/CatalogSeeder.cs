using Roselie.Core.Catalog;
using Roselie.Core.Models;
namespace Roselie.Infrastructure;
internal static class CatalogSeeder
{
    public static void Seed(SalonDbContext db)
    {
        var seed = InitialCatalog.Create();
        SeedOnce(db, seed.Categories); SeedOnce(db, seed.Services); SeedOnce(db, seed.Packages); SeedOnce(db, seed.Roles); SeedOnce(db, seed.Permissions); SeedOnce(db, seed.RolePermissions); SeedOnce(db, seed.Settings);
        db.SaveChanges();
    }
    private static void SeedOnce<T>(SalonDbContext db, IEnumerable<T> entities) where T : Entity
    {
        foreach (var entity in entities) if (!db.Set<T>().Any(e => e.Id == entity.Id)) db.Add(entity);
    }
}
