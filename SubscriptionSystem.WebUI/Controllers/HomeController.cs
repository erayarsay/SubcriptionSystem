using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Application.Models;
using System.Diagnostics;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace SubscriptionSystem.WebUI.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IWalletService _walletService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IIyzicoService _iyzicoService;
    private readonly ApplicationDbContext _context;
    private readonly ICouponService _couponService;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public HomeController(
        UserManager<AppUser> userManager, 
        IWalletService walletService, 
        ISubscriptionService subscriptionService,
        IIyzicoService iyzicoService,
        ApplicationDbContext context,
        ICouponService couponService,
        INotificationService notificationService,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _walletService = walletService;
        _subscriptionService = subscriptionService;
        _iyzicoService = iyzicoService;
        _context = context;
        _couponService = couponService;
        _notificationService = notificationService;
        _webHostEnvironment = webHostEnvironment;
    }

    #region Dashboard
public async Task<IActionResult> Index()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return RedirectToAction("Login", "Account");

    ViewBag.UserFullName = user.FullName ?? "Değerli Kullanıcı";
    ViewBag.UserProfilePicture = user.ProfilePicture;
    ViewBag.UserBalance = user.WalletBalance;
    ViewBag.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

    ViewBag.ActiveSubs = await _context.Subscriptions
    .Include(s => s.Plan)
    .Where(s => s.AppUserId == user.Id && 
               (s.IsActive || s.IsFrozen) &&
               s.EndDate > DateTime.UtcNow)
    .ToListAsync();

    var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-i)).Reverse().ToList();
    var spendings = await _context.Transactions
        .Where(t => t.AppUserId == user.Id && t.IsExpense && t.Date >= last7Days.First())
        .ToListAsync();

    ViewBag.UserDays = last7Days.Select(d => d.ToString("dd MMM")).ToList();
    ViewBag.UserSpendings = last7Days.Select(d => spendings.Where(s => s.Date.Date == d.Date).Sum(s => s.Amount)).ToList();

    var notifications = await _context.Notifications
        .Where(n => n.AppUserId == user.Id && !n.IsRead) 
        .OrderByDescending(n => n.CreatedAt)
        .Take(10)
        .ToListAsync();
        
    ViewBag.Notifications = notifications;

    if (ViewBag.IsAdmin)
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        var allEarnings = await _context.Transactions
            .Where(t => !t.IsExpense && t.Date >= last7Days.First())
            .ToListAsync();
        
        ViewBag.WeeklyDays = ViewBag.UserDays;
        ViewBag.WeeklyEarnings = last7Days.Select(d => allEarnings.Where(e => e.Date.Date == d.Date).Sum(e => e.Amount)).ToList();
    }

    return View();
}
    #endregion

    #region Wallet & Subscription
    [Authorize]
    public async Task<IActionResult> Wallet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var transactions = await _walletService.GetTransactionsByUserIdAsync(user.Id);
        ViewBag.Transactions = transactions;

        return View(user);
    }

public async Task<IActionResult> Packages()
{
    var user = await _userManager.GetUserAsync(User);
    var plans = await _subscriptionService.GetAllPlansAsync();

    var userActivePlanIds = await _context.Subscriptions
        .Where(s => s.AppUserId == user!.Id && (s.IsActive || s.IsFrozen) && s.EndDate > DateTime.UtcNow) //
        .Select(s => s.PlanId)
        .ToListAsync();

    ViewBag.UserPlanIds = userActivePlanIds;
    
    return View(plans.Where(p => !p.IsDeleted).ToList());
}

[HttpPost]
public async Task<IActionResult> Subscribe(int planId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Unauthorized();

    var result = await _subscriptionService.CreateSubscriptionAsync(user.Id, planId);
    
    if (result) 
    {
        TempData["Success"] = "İşlem başarıyla tamamlandı!";
        await _notificationService.CreateNotificationAsync(user.Id, "İşlem Başarılı", "Paketiniz tanımlandı/uzatıldı.", "success");
    }
    else 
    {
        TempData["Error"] = "Sistem bir hata bildirdi ancak paketiniz güncellenmiş olabilir. Lütfen bakiyenizi kontrol edin.";
    }

    return RedirectToAction("Index");
}
    #endregion

    #region Admin Operations
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManagePlans()
    {
        var plans = await _subscriptionService.GetAllPlansAsync();
        return View(plans ?? new List<Plan>());
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult CreatePlan() => View();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePlan(Plan plan)
    {
        if (!ModelState.IsValid) return View(plan);

        var result = await _subscriptionService.CreatePlanAsync(plan);
        if (result) TempData["Success"] = "Yeni paket oluşturuldu!";
        
        return RedirectToAction("ManagePlans");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlan(Plan plan)
    {
        if (!ModelState.IsValid) return RedirectToAction("ManagePlans");

        var result = await _subscriptionService.UpdatePlanAsync(plan);
        if (result) TempData["Success"] = "Paket güncellendi!";
        else TempData["Error"] = "Güncelleme başarısız!";

        return RedirectToAction("ManagePlans");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var result = await _subscriptionService.DeletePlanAsync(id);

        if (result)
        {
            return Json(new { success = true, message = "Paket başarıyla silindi." });
        }

        return Json(new { success = false, message = "Paket bulunamadı veya silinemedi." });
    }
    #endregion

    #region Profile & PP
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }
    [HttpPost]
    public async Task<IActionResult> UpdateProfilePicture(IFormFile photo)
    {
        if (photo == null || photo.Length == 0) return RedirectToAction("Profile");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
        
        // --- NODE.JS TARZI YOL YERİNE .NET YÖNTEMİ ---
        // wwwroot klasörünün gerçek sunucu yolunu tam isabet bulur usta
        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "img/profile");
        var path = Path.Combine(uploadDir, fileName);

        if (!Directory.Exists(uploadDir)) 
            Directory.CreateDirectory(uploadDir);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        user.ProfilePicture = fileName;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Profil fotoğrafınız başarıyla güncellendi!";
        await _notificationService.CreateNotificationAsync(user.Id, "Profil Fotoğrafı", "Profil fotoğrafınız başarıyla güncellendi.", "info");

        return RedirectToAction("Profile");
    }
    #endregion

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // 1. Şifreler eşleşiyor mu kontrolü
        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Girdiğiniz yeni şifreler birbiriyle uyuşmuyor!";
            return RedirectToAction("Profile");
        }

        // 2. Identity'nin kendi güvenli şifre değiştirme motorunu ateşliyoruz
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        if (result.Succeeded)
        {
            TempData["Success"] = "Şifreniz başarıyla değiştirildi!";
            await _notificationService.CreateNotificationAsync(user.Id, "Şifre Güncellendi", "Hesap şifreniz başarıyla değiştirildi.", "info");
        }
        else
        {
            // Eski şifre yanlışsa veya yeni şifre Identity kurallarına (büyük harf, sayı vb.) uymuyorsa hatayı yakala
            var error = result.Errors.FirstOrDefault()?.Description ?? "Mevcut şifreniz hatalı olabilir.";
            TempData["Error"] = $"Şifre güncellenemedi: {error}";
        }

        return RedirectToAction("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string email, string phoneNumber, IFormFile? photo)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Ad Soyad ve E-posta alanları boş bırakılamaz!";
            return RedirectToAction("Profile");
        }

        // 1. Eğer kullanıcı yeni bir fotoğraf seçtiyse işlemleri yapalım (Yoksa eski resmi aynen kalır)
        if (photo != null && photo.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
            var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "img/profile");
            var path = Path.Combine(uploadDir, fileName);

            if (!Directory.Exists(uploadDir)) 
                Directory.CreateDirectory(uploadDir);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Kullanıcının yeni resmini nesneye set ediyoruz usta
            user.ProfilePicture = fileName;
        }

        // 2. Diğer metinsel bilgileri güncelliyoruz
        user.FullName = fullName;
        user.Email = email;
        user.UserName = email; 
        user.PhoneNumber = phoneNumber;

        // 3. Tek seferde veritabanına mermiyi basıyoruz
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            TempData["Success"] = "Profil bilgileriniz ve fotoğrafınız başarıyla güncellendi!";
            await _notificationService.CreateNotificationAsync(user.Id, "Profil Güncellendi", "Hesap bilgileriniz ve profil fotoğrafınız başarıyla yenilendi.", "info");
        }
        else
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Bilinmeyen bir hata oluştu.";
            TempData["Error"] = $"Güncelleme başarısız: {error}";
        }

        return RedirectToAction("Profile");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> DownloadTransactionHistory()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var pdfBytes = await _walletService.GenerateTransactionPdfAsync(user.Id);

        return File(pdfBytes, "application/pdf", $"Islem_Gecmisi_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public IActionResult Error(string message)
    {
        ViewBag.ErrorMessage = message;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePlanOrder([FromBody] List<PlanOrderDto> orders)
    {
        if (orders == null || !orders.Any()) return BadRequest();

        var result = await _subscriptionService.UpdatePlanOrderAsync(orders);
        
        if (result) return Ok();
        return BadRequest();
    }

    [HttpPost]
    public async Task<IActionResult> TopUpBalance(decimal amount)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var checkoutFormHtml = await _iyzicoService.InitializePaymentForm(user, amount);

        return View("TopUpBalance", checkoutFormHtml); 
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> IyzicoCallback(string token)
    {
        if (string.IsNullOrEmpty(token)) token = Request.Form["token"]!;

        var result = await _iyzicoService.CheckPaymentResult(token);

        Console.WriteLine($">>> IYZICO DÖNÜŞÜ: Status: {result.Status}, UserID: {result.UserId}, Amount: {result.Amount}");

        if (result.Status == "failure" || result.Status == "cancelled" || result.UserId <= 0)
        {
            TempData["Error"] = "Ödeme işlemi kullanıcı tarafından iptal edildi veya banka onay vermedi!";
            return RedirectToAction("Wallet");
        }

        var updateResult = await _walletService.TopUpBalanceAsync(result.UserId, result.Amount);
        
        if (updateResult) 
        {
            TempData["Success"] = "Bakiye başarıyla yüklendi!";
        } 
        else 
        {
            TempData["Error"] = "Ödeme onaylandı ama cüzdan güncellenirken sistemsel bir hata oluştu!";
        }

        return RedirectToAction("Wallet");
    }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelSubscription(int subId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Unauthorized();

    var result = await _subscriptionService.CancelSubscriptionWithRefundAsync(subId, user.Id);

    if (result)
    {
        TempData["Success"] = "Aboneliğiniz iptal edildi ve kullanmadığınız günlerin ücreti kuruşu kuruşuna cüzdanınıza iade edildi!";
        
        await _notificationService.CreateNotificationAsync(
            user.Id, 
            "Abonelik İptali ve İade", 
            "Aboneliğiniz iptal edilerek kalan kullanım tutarınız cüzdan bakiyenize yüklenmiştir.", 
            "success"
        );
    }
    else
    {
        TempData["Error"] = "İptal işlemi sırasında bir hata oluştu veya abonelik zaten süresini doldurmuş!";
        
        await _notificationService.CreateNotificationAsync(
            user.Id, 
            "İptal İşlemi Başarısız", 
            "Abonelik iptal edilemedi. Aktif bir paketiniz olduğundan emin olun.", 
            "error"
        );
    }

    return RedirectToAction("Index");
}

    [HttpGet]
    public async Task<IActionResult> GetSubscriptionDetail(int subId)
    {
        var sub = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subId);

        if (sub == null) return NotFound();

        var remaining = sub.EndDate - DateTime.UtcNow;
        string remainingText = remaining.Days > 0 
            ? $"{remaining.Days} Gün {remaining.Hours} Saat" 
            : "Süre Doldu";

        return Json(new {
            planTitle = sub.Plan.Title,
            startDate = sub.StartDate.ToString("dd.MM.yyyy"),
            endDate = sub.EndDate.ToString("dd.MM.yyyy"),
            amount = sub.Plan.Price.ToString("N2"),
            remainingTime = remainingText
        });
    }

    [HttpPost]
    public async Task<IActionResult> UseCoupon(string couponCode)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var result = await _couponService.ValidateAndUseCouponAsync(couponCode, user.Id);

        if (result.Success)
        {
            await _walletService.TopUpBalanceAsync(user.Id, result.Amount);
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction("Wallet");
    }

    public async Task<IActionResult> AllNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var allNotifs = await _context.Notifications
                .Where(n => n.AppUserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(allNotifs);
        }
    
    [HttpPost]
public async Task<IActionResult> MarkAllNotificationsAsRead()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return BadRequest();

    var notifications = await _context.Notifications
        .Where(n => n.AppUserId == user.Id && !n.IsRead)
        .ToListAsync();

    foreach (var notif in notifications)
    {
        notif.IsRead = true;
    }

    await _context.SaveChangesAsync();
    return Ok();
}

[HttpPost]
public async Task<IActionResult> FreezeSubscription(int subId)
{
    var result = await _subscriptionService.FreezeSubscriptionAsync(subId);
    if (result) 
    {
        TempData["Success"] = "Aboneliğiniz buz dolabına kaldırıldı!";
    }
    else 
    {
        TempData["Error"] = "Dondurma işlemi başarısız. Hakkınız bitmiş olabilir!";
    }
    return RedirectToAction("Index");
}

[HttpPost]
public async Task<IActionResult> UnfreezeSubscription(int subId)
{
    var result = await _subscriptionService.UnfreezeSubscriptionAsync(subId);
    
    if (result)
    {
        TempData["Success"] = "Abonelik başarıyla aktif edildi.";
    }
    else
    {
        TempData["Error"] = "Abonelik devam ettirilirken bir sorun oluştu!";
    }

    return RedirectToAction("Index");
}
}