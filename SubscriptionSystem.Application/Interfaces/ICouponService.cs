using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Application.Interfaces;

public interface ICouponService
{
    Task<(bool Success, string Message, decimal Amount)> ValidateAndUseCouponAsync(string code, int userId);
}