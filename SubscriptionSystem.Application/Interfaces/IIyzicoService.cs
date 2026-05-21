using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Application.DTOs;

namespace SubscriptionSystem.Application.Interfaces;

public interface IIyzicoService
{
    // iyzico'nun ödeme ekranı için gerekli HTML/JS kodunu döner
    Task<string> InitializePaymentForm(AppUser user, decimal amount);
    
    // Ödeme sonrası iyzico'dan gelen sonucu kontrol eder
    Task<(string Status, int UserId, decimal Amount)> CheckPaymentResult(string token);
}