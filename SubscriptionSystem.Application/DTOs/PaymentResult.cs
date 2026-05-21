namespace SubscriptionSystem.Application.DTOs;

public class PaymentResultDto
{
    public bool IsSuccess {get; set;}
    public string? Message {get; set;}
    public decimal? Amount {get; set;}
    public string? ErrorCode {get; set;}
}