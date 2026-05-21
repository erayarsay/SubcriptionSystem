using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SubscriptionSystem.Domain;

namespace SubscriptionSystem.Persistence.Context;

public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Plan> Plans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Coupon> Coupons {get; set;}
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SubscriptionFreeze> SubscriptionFreezes {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Plan>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<AppUser>().Property(u => u.WalletBalance).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
    }
}