using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.WebUI.Controllers;

[Authorize]
public class WalletController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IWalletService _walletService;

    public WalletController(UserManager<AppUser> userManager, IWalletService walletService)
    {
        _userManager = userManager;
        _walletService = walletService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var transactions = await _walletService.GetTransactionsByUserIdAsync(user.Id);

        ViewBag.Balance = user.WalletBalance;
        return View(transactions);
    }

    [HttpPost]
    public async Task<IActionResult> Deposit(decimal amount)
    {
        if (amount <= 0) return BadRequest("Geçersiz miktar!");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _walletService.DepositAsync(user.Id, amount);

        if (result) return RedirectToAction(nameof(Index));

        return BadRequest("İşlem gerçekleştirilemedi.");
    }
}