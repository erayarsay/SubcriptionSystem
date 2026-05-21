using SubscriptionSystem.Domain.Common;

namespace SubscriptionSystem.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int AppUserId { get; set; } // Hangi kullanıcıya gidecek?
    
    // İlişki için (Navigation Property)
    public AppUser? AppUser { get; set; }
    public string? Title { get; set; } // Örn: Şifre Değişikliği
    public string? Message { get; set; } // Örn: Şifreniz başarıyla güncellendi.

    public string? Type { get; set; } // Örn: success, error, info (Lila rengi buna göre ayarlarız)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false; // Okundu mu?
}