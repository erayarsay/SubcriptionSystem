using System.Globalization;
using System.Reflection;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;
using SubscriptionSystem.Application.Models;
using Microsoft.EntityFrameworkCore; 

namespace SubscriptionSystem.Application.Services;

public class WalletService : IWalletService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IOptions<IyzicoOptions> _iyzicoOpts;

    public WalletService(UserManager<AppUser> userManager, ApplicationDbContext context, IOptions<IyzicoOptions> iyzicoOpts)
    {
        _userManager = userManager;
        _context = context;
        _iyzicoOpts = iyzicoOpts;
    }

    // Interface ile birebir aynı imza (3 Parametre)
    public async Task<(string HtmlContent, string Script)> InitializeTopUpAsync(string userId, decimal amount, string callbackUrl)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Kullanıcı bulunamadı!");

        var options = GetIyzicoOptions();
        var request = CreateCheckoutFormRequest(user, amount, callbackUrl);

        var checkoutForm = await CheckoutFormInitialize.Create(request, options);

        if (checkoutForm.Status != "success")
            throw new Exception("Iyzico hatası: " + checkoutForm.ErrorMessage);

        // --- REFLECTION (KABA KUVVET) İLE VERİ ÇEKME ---
        var type = checkoutForm.GetType();
        var scriptProp = type.GetProperty("Script") ?? type.GetProperty("script");
        var htmlProp = type.GetProperty("CheckoutFormContent") ?? type.GetProperty("checkoutFormContent") ?? type.GetProperty("HtmlContent");

        string script = scriptProp?.GetValue(checkoutForm)?.ToString() ?? "";
        string html = htmlProp?.GetValue(checkoutForm)?.ToString() ?? "";

        return (html, script);
    }

    public async Task<PaymentResultDto> HandlePaymentCallbackAsync(string token)
    {
        var options = GetIyzicoOptions();
        var request = new RetrieveCheckoutFormRequest { Token = token };
        
        var result = await CheckoutForm.Retrieve(request, options);

        if (result.Status != "success")
            return new PaymentResultDto { IsSuccess = false, Message = result.ErrorMessage };

        // BasketId içine UserId koymuştuk
        var user = await _userManager.FindByIdAsync(result.BasketId);
        if (user == null) throw new Exception("Kullanıcı bulunamadı!");

        decimal amount = decimal.Parse(result.PaidPrice, CultureInfo.InvariantCulture);
        
        user.WalletBalance += amount;
        _context.Transactions.Add(new Transaction {
            AppUserId = user.Id,
            Amount = amount,
            Date = DateTime.UtcNow,
            IsExpense = false,
            Description = "Iyzico Bakiye Yükleme"
        });

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return new PaymentResultDto { IsSuccess = true, Amount = amount };
    }

    private Iyzipay.Options GetIyzicoOptions() => new Iyzipay.Options {
        ApiKey = _iyzicoOpts.Value.ApiKey,
        SecretKey = _iyzicoOpts.Value.SecretKey,
        BaseUrl = _iyzicoOpts.Value.BaseUrl
    };

    private CreateCheckoutFormInitializeRequest CreateCheckoutFormRequest(AppUser user, decimal amount, string callbackUrl)
    {
        string price = amount.ToString("F2", CultureInfo.InvariantCulture);
        string fullName = user.FullName ?? "Değerli Kullanıcı";
        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var request = new CreateCheckoutFormInitializeRequest {
            Locale = Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            Price = price,
            PaidPrice = price,
            Currency = Currency.TRY.ToString(),
            BasketId = user.Id.ToString(), 
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = callbackUrl
        };

        request.Buyer = new Buyer {
            Id = user.Id.ToString(),
            Name = nameParts[0],
            Surname = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Soyad Yok",
            Email = user.Email,
            GsmNumber = "+90000000000",
            IdentityNumber = "11111111111",
            RegistrationAddress = "Denizli",
            Ip = "127.0.0.1", // IP'yi burada sabitledik
            City = "Denizli",
            Country = "Turkey"
        };

        var address = new Address {
            ContactName = fullName,
            City = "Denizli",
            Country = "Turkey",
            Description = "Müşteri Adresi"
        };
        request.BillingAddress = address;
        request.ShippingAddress = address;

        request.BasketItems = new List<BasketItem> {
            new BasketItem {
                Id = "ITEM-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "Cüzdan Yükleme",
                Category1 = "Wallet",
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Price = price
            }
        };

        return request;
    }
    public async Task<List<Transaction>> GetTransactionsByUserIdAsync(int userId)
{
    return await _context.Transactions
        .Where(t => t.AppUserId == userId)
        .OrderByDescending(t => t.Date)
        .ToListAsync();
}

public async Task<bool> DepositAsync(int userId, decimal amount)
{
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user == null) return false;

    user.WalletBalance += amount;
    _context.Transactions.Add(new Transaction {
        AppUserId = userId,
        Amount = amount,
        Date = DateTime.UtcNow,
        IsExpense = false,
        Description = "Bakiye Yüklendi (Manuel)"
    });

    await _userManager.UpdateAsync(user);
    await _context.SaveChangesAsync();
    return true;
}

public async Task<byte[]> GenerateTransactionPdfAsync(int userId)
{
    var transactions = await _context.Transactions
        .Where(t => t.AppUserId == userId)
        .OrderByDescending(t => t.Date)
        .ToListAsync();

    using (var ms = new MemoryStream())
    {
        using (var sw = new StreamWriter(ms))
        {
            sw.WriteLine("ISLEM GECMISI RAPORU");
            sw.WriteLine("--------------------");
            foreach (var item in transactions)
            {
                sw.WriteLine($"{item.Date:dd/MM/yyyy HH:mm} | {item.Id} | {item.Amount:N2} TL | {item.Description}");
            }
            sw.Flush();
            return ms.ToArray();
        }
    }
}

public async Task<bool> TopUpBalanceAsync(int userId, decimal amount)
{
    if (amount <= 0) return false;

    // 1. Kullanıcıyı bul (UserManager yerine direkt Context'ten çekelim ki takip etsin)
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return false;

    try 
    {
        // 2. Bakiyeyi güncelle
        user.WalletBalance += amount;

        // 3. Transaction ekle
        var transaction = new Transaction 
        {
            AppUserId = userId,
            Amount = amount,
            Date = DateTime.UtcNow, 
            IsExpense = false, 
            Description = "iyzico ile bakiye yüklendi"
        };

        _context.Transactions.Add(transaction);
        
        // 4. TEK SEFERDE KAYDET (Racon budur)
        // Bu satır hem kullanıcıyı hem transaction'ı aynı anda veritabanına yazar.
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }
    catch (Exception ex)
    {
        // Terminalde hatanın ne olduğunu şak diye görelim usta
        Console.WriteLine("CÜZDAN GÜNCELLEME HATASI: " + ex.Message);
        if (ex.InnerException != null) 
            Console.WriteLine("DETAY: " + ex.InnerException.Message);
            
        return false;
    }
}

}