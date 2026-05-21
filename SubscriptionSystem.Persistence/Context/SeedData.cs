using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Persistence.Context;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // --- 1. ROL OLUŞTURMA BÖLÜMÜ ---
        string[] roles = { "Admin", "User" };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                Console.WriteLine($">>> {roleName} rolü veritabanına eklendi.");
            }
        }

        // --- 2. ADMIN KULLANICISI OLUŞTURMA BÖLÜMÜ ---
        var adminEmail = configuration["AdminSettings:Email"];
        var adminPassword = configuration["AdminSettings:Password"];
        var adminFullName = configuration["AdminSettings:adminFullName"];

        if (string.IsNullOrEmpty(adminEmail)) return;

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminFullName ?? "Admin User",
                WalletBalance = 1000000,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(user, adminPassword!);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine($">>> Admin ({adminEmail}) başarıyla oluşturuldu.");
            }
            else 
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($">>> Admin Hatası: {error.Description}");
                }
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}