using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    public int AppUserId {get; set;}
    public virtual AppUser AppUser {get; set;} = null!;

    public decimal Amount {get; set;}
    public DateTime TransactionDate {get; set;} = DateTime.UtcNow;

    public string TransactionType {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;
}