namespace SubscriptionSystem.Application.DTOs;

public class NotificationDto
{
    public string? Message {get; set;}
    public string? Type {get; set;}
    public DateTime SentDate {get; set;} = DateTime.UtcNow;
}