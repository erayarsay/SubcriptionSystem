using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Services;

public class IyzicoService : IIyzicoService
{
    private readonly Options _options;
    private readonly IConfiguration _configuration;

    public IyzicoService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // appsettings.json'daki verileri çekiyoruz usta
        _options = new Options
        {
            ApiKey = _configuration["Iyzico:ApiKey"],
            SecretKey = _configuration["Iyzico:SecretKey"],
            BaseUrl = _configuration["Iyzico:BaseUrl"]
        };
    }

    public async Task<string> InitializePaymentForm(AppUser user, decimal amount)
{
    CreateCheckoutFormInitializeRequest request = new CreateCheckoutFormInitializeRequest();
    request.Locale = Locale.TR.ToString();
    
    // 1. GARANTİ: UserId'yi her iki alana da gömüyoruz ki iyzico yutamasın.
    request.ConversationId = user.Id.ToString(); 
    request.BasketId = user.Id.ToString(); // Burası dönüşte (CheckPaymentResult) hayat kurtaracak.

    request.Price = amount.ToString("F2").Replace(",", ".");
    request.PaidPrice = amount.ToString("F2").Replace(",", ".");
    request.Currency = Currency.TRY.ToString();
    request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
    
    // Ödeme bitince iyzico'nun ateş edeceği URL
    request.CallbackUrl = "http://localhost:5246/Home/IyzicoCallback";

    // Alıcı Bilgileri
    Buyer buyer = new Buyer();
    buyer.Id = user.Id.ToString();
    // İsim boş gelirse dükkan patlamasın diye yedek isimler
    buyer.Name = user.FullName?.Split(' ')[0] ?? "Eray";
    buyer.Surname = user.FullName?.Contains(" ") == true ? user.FullName.Split(' ').Last() : "Arsay";
    buyer.Email = user.Email;
    buyer.IdentityNumber = "11111111111"; // Sandbox için sabit 11 hane
    buyer.RegistrationAddress = "Denizli";
    buyer.Ip = "85.34.78.112";
    buyer.City = "Denizli";
    buyer.Country = "Turkey";
    request.Buyer = buyer;

    // Adres Bilgileri
    Address billingAddress = new Address();
    billingAddress.ContactName = user.FullName ?? "Eray Arsay";
    billingAddress.City = "Denizli";
    billingAddress.Country = "Turkey";
    billingAddress.Description = "Merkez";
    request.BillingAddress = billingAddress;

    // Sepet İçeriği (İyzico en az 1 ürün bekler usta)
    List<BasketItem> basketItems = new List<BasketItem>();
    BasketItem firstBasketItem = new BasketItem();
    firstBasketItem.Id = "W101"; // Wallet 101
    firstBasketItem.Name = "Cüzdan Bakiye Yükleme";
    firstBasketItem.Category1 = "Wallet";
    firstBasketItem.ItemType = BasketItemType.VIRTUAL.ToString();
    firstBasketItem.Price = amount.ToString("F2").Replace(",", ".");
    basketItems.Add(firstBasketItem);
    request.BasketItems = basketItems;

    // İyzico'dan form kodlarını istiyoruz
    CheckoutFormInitialize checkoutFormInitialize = await CheckoutFormInitialize.Create(request, _options);
    
    // Usta, bu string senin TopUpBalance.cshtml sayfandaki @Html.Raw(Model) kısmına mermi gibi gidecek
    return checkoutFormInitialize.CheckoutFormContent;
}

public async Task<(string Status, int UserId, decimal Amount)> CheckPaymentResult(string token)
{
    RetrieveCheckoutFormRequest request = new RetrieveCheckoutFormRequest();
    request.Token = token;

    // İyzico'dan form sonucunu çekiyoruz usta
    CheckoutForm checkoutForm = await CheckoutForm.Retrieve(request, _options);

    // --- USTA GÜVENLİK DUVARI BURASI ---
    // 1. Genel durum 'success' olmalı
    // 2. checkoutForm null olmamalı
    // 3. ASIL KRİTİK NOKTA: PaymentStatus kesinlikle 'SUCCESS' olmalı (Banka onayı)
    if (checkoutForm != null && 
        checkoutForm.Status == "success" && 
        checkoutForm.PaymentStatus == "SUCCESS")
    {
        // Önce BasketId'ye bak, olmazsa ConversationId'ye bak
        string rawUserId = checkoutForm.BasketId ?? checkoutForm.ConversationId;
        
        int.TryParse(rawUserId, out int userId);
        decimal.TryParse(checkoutForm.PaidPrice, out decimal amount);

        return ("success", userId, amount);
    }

    // Eğer kullanıcı iptal ettiyse veya kartta para yoksa PaymentStatus 'SUCCESS' gelmez
    // Konsola log basalım ki test ederken ne döndüğünü terminalde canlı gör usta
    if (checkoutForm != null)
    {
        Console.WriteLine($">>> ÖDEME REDDEDİLDİ VEYA İPTAL EDİLDİ! Form Status: {checkoutForm.Status}, Payment Status: {checkoutForm.PaymentStatus}");
    }

    return ("failed", 0, 0);
}
}