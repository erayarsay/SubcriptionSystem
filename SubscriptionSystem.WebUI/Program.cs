using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SubscriptionSystem.Persistence.Context;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.Services;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.WebUI.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- 🚀 SERILOG ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Veritabanı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity
builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options => {
    options.Password.RequiredLength = 6;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. MVC
builder.Services.AddControllersWithViews();

// --- 🛡️ 4. DEPENDENCY INJECTION (DÜZELTİLDİ) ---
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IWalletService, WalletService>(); // Arayüz -> Servis eşleşmesi düzeltildi

// 5. Iyzico Ayarları
builder.Services.Configure<SubscriptionSystem.Application.Models.IyzicoOptions>(builder.Configuration.GetSection("Iyzico"));
builder.Services.AddScoped<IIyzicoService, IyzicoService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<INotificationService, NotificationService>(); // BU SATIRI EKLE USTA!
var app = builder.Build();

// --- 🛡️ 6. MIDDLEWARE ---
app.UseMiddleware<GlobalExceptionMiddleware>(); 

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// 8. Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
        Console.WriteLine(">>> Sistem ve SeedData hazır.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(">>> SeedData hatası: " + ex.Message);
    }
}

app.Run();