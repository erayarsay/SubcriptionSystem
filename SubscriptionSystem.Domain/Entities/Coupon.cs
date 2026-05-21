using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain;

public class Coupon
{
    public int Id {get; set;}
    public string? Code {get; set;}
    public decimal DiscountAmount {get; set;}
    public DateTime ExpiryDate {get; set;}
    public bool IsUsed {get; set;} = false;
    public int? UsedByUserId {get; set;}
}