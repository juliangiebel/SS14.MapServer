using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SS14.MapServer.Helpers;

//Taken from https://gist.github.com/Tim-Hodge/eea0601a14177c199fe60557eeeff31e
public sealed class StartupMigrationHelper
{
    public void Migrate<TContext>(IApplicationBuilder builder) where TContext : DbContext
    {
        using var scope = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var ctx = scope.ServiceProvider.GetRequiredService<TContext>();

        var sp = ctx.GetInfrastructure();

        var modelDiffer = sp.GetRequiredService<IMigrationsModelDiffer>();
        var migrationsAssembly = sp.GetRequiredService<IMigrationsAssembly>();

        var modelInitializer = sp.GetRequiredService<IModelRuntimeInitializer>();
        var sourceModel = modelInitializer.Initialize(migrationsAssembly.ModelSnapshot!.Model);

        var designTimeModel = sp.GetRequiredService<IDesignTimeModel>();
        var readOptimizedModel = designTimeModel.Model;

        var diffsExist = modelDiffer.HasDifferences(
            sourceModel.GetRelationalModel(),
            readOptimizedModel.GetRelationalModel());

        if(diffsExist)
        {
            throw new InvalidOperationException("There are differences between the current database model and the most recent migration.");
        }

        ctx.Database.Migrate();
    }
}
