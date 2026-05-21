using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using SubscriptionSystem.Persistence.Context;

namespace SubscriptionSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type)
    {
        var notification = new Notification
        {
            AppUserId = userId,
            Title = title,
            Message = message,
            Type = type, // "Success", "Error", "Info"
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.AppUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10) // Son 10 bildirim yeter usta
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notif = await _context.Notifications.FindAsync(notificationId);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}