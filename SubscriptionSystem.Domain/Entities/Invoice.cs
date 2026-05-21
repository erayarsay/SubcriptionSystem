using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class Invoice : BaseEntity
{
    public int SubscriptionId {get; set;}
    public Subscription Subscription {get; set;} = null!;

    public decimal Amount {get; set;}
    public DateTime DueDate {get; set;}
    public bool IsPaid {get; set;}
}