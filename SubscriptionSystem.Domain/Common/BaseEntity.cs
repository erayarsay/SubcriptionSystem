namespace SubscriptionSystem.Domain.Common;

public abstract class BaseEntity
{
    public int Id {get; set;}
    public DateTime CreateDate {get; set;} = DateTime.UtcNow;
    public DateTime? UpdatedDate {get; set;}
    public bool IsDeleted {get; set;} = false;
}