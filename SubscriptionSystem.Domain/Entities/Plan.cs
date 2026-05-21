using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class Plan : BaseEntity
{
    public string Title { get; set; } = null!;
    public string SubTitle { get; set; } = string.Empty; 
    public string Description { get; set; } = null!; 
    public decimal Price { get; set; }
    public int DuraitonInMonths { get; set; }
    public bool IsPopular { get; set; } = false;
    public int OrderIndex { get; set; }
}