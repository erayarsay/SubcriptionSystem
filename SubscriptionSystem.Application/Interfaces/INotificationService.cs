using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Application.DTOs;

namespace SubscriptionSystem.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, string title, string message, string type);
    Task<List<Notification>> GetUserNotificationsAsync(int userId);
    
    // Bildirimleri okundu işaretler
    Task MarkAsReadAsync(int notificationId);
}