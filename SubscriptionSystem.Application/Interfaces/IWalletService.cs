using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Application.Interfaces;

public interface IWalletService
{
    Task<(string HtmlContent, string Script)> InitializeTopUpAsync(string userId, decimal amount, string callbackUrl);
    Task<PaymentResultDto> HandlePaymentCallbackAsync(string token);
    Task<List<Transaction>> GetTransactionsByUserIdAsync(int userId);
    Task<bool> DepositAsync(int userId, decimal amount);
    Task<byte[]> GenerateTransactionPdfAsync(int userId);
    Task<bool> TopUpBalanceAsync(int userId, decimal amount);
}