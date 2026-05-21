using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=SubscriptionDb;Username=postgres;Password=1234");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}