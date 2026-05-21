using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class Subscription : BaseEntity
{
    // Foreign Key'ler
    public int AppUserId { get; set; }
    public virtual AppUser AppUser { get; set; } = null!; // virtual olması lazy loading için iyidir

    public int PlanId { get; set; }
    public virtual Plan Plan { get; set; } = null!;

    // Abonelik Detayları
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool AutoRenew { get; set; } = true;
    public int RemainingFreezeCount {get; set;} = 3;
    public bool IsFrozen {get; set;} = false;
}