using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class SubscriptionFreeze : BaseEntity
{
    public int SubscriptionId {get; set;}
    public Subscription? Subscription {get; set;}
    public DateTime FreezeStartDate {get; set;}
    public DateTime? FreezeEndDate {get; set;}
    public bool IsActive {get; set;}
}