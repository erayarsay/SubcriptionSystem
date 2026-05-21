using System.IO.Compression;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Application.Services;

public class CouponService : ICouponService
{
    private readonly ApplicationDbContext _context;

    public CouponService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, decimal Amount)> ValidateAndUseCouponAsync(string code, int userId)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper() && !c.IsUsed);

        if(coupon == null) return(false, "Geçersiz veya kullanılmış kupon!", 0);
        if (coupon.ExpiryDate < DateTime.UtcNow) return (false, "Kupon süresi dolmuş!", 0);

        coupon.IsUsed = true;
        coupon.UsedByUserId = userId;

        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync();

        return (true, "Kupon başarıyla uygulandı!" , coupon.DiscountAmount);
    }
}