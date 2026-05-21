using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.WebUI.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() => View();


[HttpPost]
public async Task<IActionResult> Register(RegisterDto dto)
{
    if (ModelState.IsValid)
    {
        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            WalletBalance = 0,
            EmailConfirmed = true 
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            TempData["SuccessMessage"] = "Kaydınız başarıyla tamamlandı. Lütfen giriş yapın.";
            return RedirectToAction("Login", "Account");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
    }
    return View(dto);
}
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
{
    if (!ModelState.IsValid)
    {
        Console.WriteLine(">>> HATA: ModelState geçerli değil! Form verileri DTO'ya ulaşamıyor.");
        return View(dto);
    }

    var user = await _userManager.FindByEmailAsync(dto.Email);
    
    if (user != null)
    {
        var result = await _signInManager.PasswordSignInAsync(user.UserName!, dto.Password, false, false);
        
        if (result.Succeeded)
        {
            Console.WriteLine($">>> GİRİŞ BAŞARILI: {dto.Email}");
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut) Console.WriteLine(">>> HESAP KİLİTLİ!");
        if (result.IsNotAllowed) Console.WriteLine(">>> GİRİŞE İZİN VERİLMEDİ (Email onaylanmamış olabilir)!");
        if (result.RequiresTwoFactor) Console.WriteLine(">>> İKİ ADIMLI DOĞRULAMA GEREKİYOR!");
    }
    else
    {
        Console.WriteLine($">>> HATA: Veritabanında '{dto.Email}' adında bir kullanıcı yok!");
    }

    ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
    Console.WriteLine($">>> GİRİŞ BAŞARISIZ: {dto.Email}");
    return View(dto);
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
}