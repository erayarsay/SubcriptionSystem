using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public SubscriptionService(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.AppUser)
            .Include(s => s.Plan)
            .ToListAsync();
    }

    public async Task<bool> AddSubscriptionAsync(CreateSubscriptionDto dto)
    {
        var plan = await _context.Plans.FindAsync(dto.PlanId);
        var user = await _context.Users.FindAsync(dto.AppUserId);

        if (plan == null || user == null) return false;
        if (user.WalletBalance < plan.Price) return false;

        user.WalletBalance -= plan.Price;

        // 1. Önce değişkeni tanımlıyoruz (İsme dikkat: 'transaction')
        var transaction = new Transaction // Veritabanındaki tablo adın 'Transaction' ise böyle kalsın
        {
            AppUserId = user.Id,
            Amount = plan.Price,
            Date = DateTime.UtcNow,
            IsExpense = true,
            Description = $"{plan.Title} aboneliği satın alındı."
        };

        var subscription = new Subscription
        {
            AppUserId = user.Id,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(plan.DuraitonInMonths),
            IsActive = true
        };

        // 2. Şimdi listeye ekliyoruz
        _context.Transactions.Add(transaction); // Burada 'transaction' ismini kullandık
        _context.Subscriptions.Add(subscription);
        
        await _context.SaveChangesAsync();
        return true;
    }

    // 2. Interface'in ağladığı o "eksik" metot (Sözleşmeyi tamamlamak için ekledik)
    public async Task<bool> CreateSubscriptionAsync(int userId, int planId)
    {
        // 1. Kullanıcıyı ve Planı bulalım usta
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var plan = await _context.Plans.FindAsync(planId);

        if (user == null || plan == null) return false;

        // 2. Bakiye kontrolü (Para yoksa işlem de yok)
        if (user.WalletBalance < plan.Price) return false;

        // 3. BAKİYE DÜŞME (Identity'yi beklemeden nesne üzerinden düşüyoruz)
        user.WalletBalance -= plan.Price;

        // 4. AYNI PAKET VAR MI KONTROLÜ (Uzatma Mantığı)
        var existingSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.AppUserId == userId && s.PlanId == planId && (s.IsActive || s.IsFrozen));

        if (existingSub != null)
        {
            // Varsa süreyi uzatıyoruz usta
            existingSub.EndDate = existingSub.EndDate.AddDays(plan.DuraitonInMonths);
            existingSub.IsActive = true; 
            existingSub.IsFrozen = false; // Dondurulmuşsa buzu çözülsün
            existingSub.UpdatedDate = DateTime.UtcNow;
            _context.Subscriptions.Update(existingSub);
        }
        else
        {
            // Yoksa tertemiz yeni kayıt
            var subscription = new Subscription
            {
                AppUserId = userId,
                PlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.DuraitonInMonths),
                IsActive = true,
                CreateDate = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
        }

        // 5. İşlem Geçmişi (Transaction) Kaydı
        var transaction = new Transaction
        {
            AppUserId = userId,
            Amount = plan.Price,
            Date = DateTime.UtcNow,
            IsExpense = true,
            Description = $"{plan.Title} paketi {(existingSub != null ? "uzatıldı" : "satın alındı")}."
        };
        _context.Transactions.Add(transaction);

        // Identity ve DbContext'i tek potada eritiyoruz usta
        // UserManager zaten alttaki DbContext'i kullanıyor.
        // Önce Identity güncellemesini yapıyoruz.
        var identityResult = await _userManager.UpdateAsync(user);
        
        if (!identityResult.Succeeded) return false;

        // SaveChangesAsync burada abonelik (insert/update) ve transaction (insert) işlemlerini yapar.
        // Eğer sadece mevcut abonelik güncellendiyse ve Identity bunu hallettiyse burası 0 dönebilir.
        // O yüzden buradaki sonucu "işlem başarısız" saymak yerine sadece çalıştırıyoruz.
        await _context.SaveChangesAsync();
        
        // Identity başarılı olduysa ve kod buraya kadar geldiyse bu iş mermidir.
        return true;
    }

    // 3. İptal etme
    public async Task<bool> CancelSubscriptionAsync(int subscriptionId)
    {
        var sub = await _context.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return false;

        sub.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelSubscriptionWithRefundAsync(int subscriptionId, int userId)
    {
        // 1. İptal edilecek aboneliği ve planı bulalım usta
        var sub = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.AppUserId == userId && s.IsActive);

        if (sub == null || sub.Plan == null) return false;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        // 2. Finansal Matematik (Prorated Tutar Hesabı)
        var now = DateTime.UtcNow;
        
        // Eğer paket süresi zaten bir şekilde dolmuşsa iade falan yok
        if (now >= sub.EndDate) return false;

        // Toplam süreyi ve kalan süreyi gün bazında buluyoruz
        double totalDays = (sub.EndDate - sub.StartDate).TotalDays;
        double remainingDays = (sub.EndDate - now).TotalDays;

        // Güvenlik kontrolü: Toplam gün sıfır veya negatifse patlamasın
        if (totalDays <= 0 || remainingDays <= 0) return false;

        // Günlük birim fiyatı bulup kalan günle çarpıyoruz usta
        decimal dailyPrice = sub.Plan.Price / (decimal)totalDays;
        decimal refundAmount = dailyPrice * (decimal)remainingDays;

        // Kuruş hesabı yuvarlama yapalım (Örn: 180.45 ₺)
        refundAmount = Math.Round(refundAmount, 2);

        // 3. Veritabanı Güncelleme Operasyonu
        // Aboneliği kapatıyoruz
        sub.IsActive = false;
        sub.UpdatedDate = now;
        _context.Subscriptions.Update(sub);

        // Parayı kullanıcının cüzdanına geri yüklüyoruz usta
        user.WalletBalance += refundAmount;
        var identityResult = await _userManager.UpdateAsync(user);
        if (!identityResult.Succeeded) return false;

        // İşlem geçmişine (Transactions) Gelir olarak kaydediyoruz (IsExpense = false)
        var transaction = new Transaction
        {
            AppUserId = userId,
            Amount = refundAmount,
            Date = now,
            IsExpense = false, // Bu bir harcama değil, iade (gelir)
            Description = $"{sub.Plan.Title} aboneliği iptal edildi. Kalan {Math.Ceiling(remainingDays)} günün iadesi cüzdana yüklendi."
        };
        _context.Transactions.Add(transaction);

        // Hepsini tek seferde DB'ye mermi gibi basıyoruz
        return await _context.SaveChangesAsync() > 0;
    }

    // 4. Detay getirme mermisi
    public async Task<object> GetSubscriptionDetailAsync(int userId, int subId)
{
    var sub = await _context.Subscriptions
        .Include(s => s.Plan)
        .FirstOrDefaultAsync(s => s.Id == subId && s.AppUserId == userId);

    if (sub == null) throw new Exception("Abonelik detayı bulunamadı!");

    // CS8602 Çözümü: sub.Plan? (Plan null ise devam etme)
    var transaction = await _context.Transactions
        .Where(t => t.AppUserId == userId && 
                    t.IsExpense && 
                    t.Description != null && // Description null kontrolü
                    sub.Plan != null &&      // Plan null kontrolü
                    t.Description.Contains(sub.Plan.Title))
        .OrderByDescending(t => t.Date)
        .FirstOrDefaultAsync();

    var remaining = sub.EndDate - DateTime.UtcNow;
    string remainingStr = remaining.Ticks <= 0 ? "Süre Doldu" : $"{(int)remaining.TotalDays} Gün {remaining.Hours} Saat";

    return new {
        // sub.Plan? diyerek plan null olsa bile patlamasını engelliyoruz
        planTitle = sub.Plan?.Title ?? "Bilinmeyen Plan", 
        startDate = sub.StartDate.ToString("dd.MM.yyyy"),
        endDate = sub.EndDate.ToString("dd.MM.yyyy"),
        amount = transaction?.Amount ?? (sub.Plan?.Price ?? 0),
        remainingTime = remainingStr
    };
}
    // 5. Otomatik yenileme anahtarı
    public async Task<bool> ToggleAutoRenewAsync(int userId, int subId, bool status)
    {
        var sub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subId && s.AppUserId == userId);
        
        if (sub == null) return false;

        sub.AutoRenew = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Subscription>> GetActiveSubscriptionsByUserAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.AppUserId == userId && s.EndDate > DateTime.UtcNow && s.IsActive)
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();
    }

    public async Task<Subscription?> GetExpiringSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.AppUserId == userId && 
                        s.IsActive && 
                        !s.AutoRenew && 
                        s.EndDate > DateTime.UtcNow && 
                        s.EndDate <= DateTime.UtcNow.AddDays(3))
        .OrderBy(s => s.EndDate)
        .FirstOrDefaultAsync();
    }


    public async Task<bool> UpdatePlanAsync(Plan plan)
{
    var existingPlan = await _context.Plans.FindAsync(plan.Id);
    if (existingPlan == null) return false;

    existingPlan.Title = plan.Title;
    existingPlan.SubTitle = plan.SubTitle;
    existingPlan.Description = plan.Description;
    existingPlan.Price = plan.Price;
    existingPlan.DuraitonInMonths = plan.DuraitonInMonths;
    existingPlan.IsPopular = plan.IsPopular;
    existingPlan.UpdatedDate = DateTime.UtcNow;

    _context.Plans.Update(existingPlan);
    return await _context.SaveChangesAsync() > 0;
}

public async Task<bool> DeletePlanAsync(int id)
{
    var plan = await _context.Plans.FindAsync(id);
    if (plan == null) return false;

    _context.Plans.Remove(plan);
    var result = await _context.SaveChangesAsync();
    
    return result > 0;
}

// 1. Tek bir planı ID ile getirme (Düzenleme ekranı için lazım)
public async Task<Plan?> GetPlanByIdAsync(int id)
{
    return await _context.Plans.FindAsync(id);
}

// 2. Yeni plan oluşturma (Yönetim panelinden ekleme için)
public async Task<bool> CreatePlanAsync(Plan plan)
{
    plan.CreateDate = DateTime.UtcNow;
    await _context.Plans.AddAsync(plan);
    return await _context.SaveChangesAsync() > 0;
}

// 3. Tüm planları getirme (Yönetim panelinde listelemek için)
public async Task<List<Plan>> GetAllPlansAsync()
{
    return await _context.Plans
        .Where(p => !p.IsDeleted)
        .OrderBy(p => p.OrderIndex) // Önce buna göre diz usta
        .ThenByDescending(p => p.CreateDate) 
        .ToListAsync();
}

public async Task<bool> UpdatePlanOrderAsync(List<PlanOrderDto> orders)
{
    foreach (var item in orders)
    {
        var plan = await _context.Plans.FindAsync(item.Id);
        if (plan != null)
        {
            plan.OrderIndex = item.Order;
            _context.Plans.Update(plan);
        }
    }
    return await _context.SaveChangesAsync() > 0;
}

public async Task<bool> FreezeSubscriptionAsync(int subId)
{
    var sub = await _context.Subscriptions.FindAsync(subId);
    
    // Usta, veritabanında RemainingFreezeCount 0 ise buraya takılır!
    if (sub == null || sub.IsFrozen || sub.RemainingFreezeCount <= 0) return false;

    sub.IsFrozen = true;
    sub.IsActive = false; 
    sub.RemainingFreezeCount -= 1;
    sub.UpdatedDate = DateTime.UtcNow;

    var freezeLog = new SubscriptionFreeze
    {
        SubscriptionId = sub.Id,
        FreezeStartDate = DateTime.UtcNow,
        IsActive = true
    };

    _context.SubscriptionFreezes.Add(freezeLog);
    _context.Subscriptions.Update(sub); // Değişikliği zorla bildiriyoruz
    
    return await _context.SaveChangesAsync() > 0;
}

public async Task<bool> UnfreezeSubscriptionAsync(int subId)
{
    var sub = await _context.Subscriptions.FindAsync(subId);
    var freeze = await _context.SubscriptionFreezes
        .FirstOrDefaultAsync(f => f.SubscriptionId == subId && f.IsActive);

    if (sub == null || freeze == null) return false;

    // Kaç gün dondurulmuş kalmış?
    var frozenDays = (DateTime.UtcNow - freeze.FreezeStartDate).Days;
    
    // Bitiş tarihini dondurulan süre kadar ileri atıyoruz usta
    sub.EndDate = sub.EndDate.AddDays(frozenDays);
    sub.IsFrozen = false;
    sub.IsActive = true;

    freeze.FreezeEndDate = DateTime.UtcNow;
    freeze.IsActive = false;

    await _context.SaveChangesAsync();
    return true;
}
}