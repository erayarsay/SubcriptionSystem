using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public bool IsExpense { get; set; }
    public AppUser? AppUser { get; set; }
}