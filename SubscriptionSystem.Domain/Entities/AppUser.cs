using Microsoft.AspNetCore.Identity; // Identity için şart
using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public decimal WalletBalance { get; set; }
    public string? ProfilePicture {get; set;}

    public ICollection<Subscription>? Subscriptions { get; set; }
}