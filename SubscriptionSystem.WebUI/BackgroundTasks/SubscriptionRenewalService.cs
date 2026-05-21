using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Persistence.Context;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.WebUI.BackgroundTasks;

public class SubscriptionRenewalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    public SubscriptionRenewalService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                // Süresi bugün biten ve AutoRenew'u açık olan aktif abonelikleri bul usta
                var expiredSubs = await context.Subscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.AppUser)
                    .Where(s => s.IsActive && s.AutoRenew && s.EndDate <= now)
                    .ToListAsync();

                foreach (var sub in expiredSubs)
                {
                    // Bakiyesi paketi almaya yetiyor mu?
                    if (sub.AppUser.WalletBalance >= sub.Plan.Price)
                    {
                        // 1. Parayı çek
                        sub.AppUser.WalletBalance -= sub.Plan.Price;
                        
                        // 2. Süreyi uzat (Örn: 30 gün)
                        sub.EndDate = sub.EndDate.AddDays(sub.Plan.DuraitonInMonths);
                        
                        // 3. Muhasebe kaydı ekle
                        context.Transactions.Add(new Transaction {
                            AppUserId = sub.AppUserId,
                            Amount = sub.Plan.Price,
                            Description = $"Otomatik Yenileme: {sub.Plan.Title}",
                            Date = DateTime.UtcNow,
                            IsExpense = true
                        });
                        
                        Console.WriteLine($">>> {sub.AppUser.UserName} kullanıcısının {sub.Plan.Title} paketi otomatik yenilendi!");
                    }
                    else
                    {
                        // Parası yoksa paketi kapatıyoruz usta
                        sub.IsActive = false;
                        Console.WriteLine($">>> {sub.AppUser.UserName} bakiyesi yetersiz, paket iptal edildi.");
                    }
                }
                await context.SaveChangesAsync();
            }

            // Her 1 saatte bir kontrol et (Test için süreyi kısaltabilirsin usta)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}