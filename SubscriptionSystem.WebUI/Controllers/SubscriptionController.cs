using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ToListAsync için bu şart!
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;
using Microsoft.AspNetCore.Mvc.Rendering; // SelectList'i kısaltmak için

namespace SubscriptionSystem.WebUI.Controllers;

public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ApplicationDbContext _context;

    public SubscriptionController(ISubscriptionService subscriptionService, ApplicationDbContext context)
    {
        _subscriptionService = subscriptionService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
        return View(subscriptions);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // Planları çekiyoruz
        var plans = await _context.Plans.ToListAsync();
        
        // ViewBag'e planları dolduruyoruz
        ViewBag.Plans = new SelectList(plans, "Id", "Name");
        
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSubscriptionDto dto)
    {
        if (ModelState.IsValid)
        {
            await _subscriptionService.AddSubscriptionAsync(dto);
            return RedirectToAction(nameof(Index));
        }
        var plans = await _context.Plans.ToListAsync();
        ViewBag.Plans = new SelectList(plans, "Id", "Name");
        
        return View(dto);
    }
}