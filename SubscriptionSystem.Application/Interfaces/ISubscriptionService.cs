using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Application.DTOs;

namespace SubscriptionSystem.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<Subscription>> GetAllSubscriptionsAsync();
    Task<bool> AddSubscriptionAsync(CreateSubscriptionDto dto);
    Task<bool> CreateSubscriptionAsync(int userId, int planId);
    Task<bool> CancelSubscriptionAsync(int subscriptionId);
    Task<List<Subscription>> GetActiveSubscriptionsByUserAsync(int userId);
    Task<Subscription?> GetExpiringSubscriptionAsync(int userId);
    Task<object> GetSubscriptionDetailAsync(int userId, int subId);
    Task<bool> ToggleAutoRenewAsync(int userId, int subId, bool status);
    Task<Plan?> GetPlanByIdAsync(int id);
    Task<bool> CreatePlanAsync(Plan plan);
    Task<bool> UpdatePlanAsync(Plan plan);
    Task<bool> DeletePlanAsync(int id);
    Task<List<Plan>> GetAllPlansAsync();
    Task<bool> UpdatePlanOrderAsync(List<PlanOrderDto> orders);
    Task<bool> FreezeSubscriptionAsync(int subId);
    Task<bool> UnfreezeSubscriptionAsync(int subId);
    Task<bool> CancelSubscriptionWithRefundAsync(int subscriptionId, int userId);
}