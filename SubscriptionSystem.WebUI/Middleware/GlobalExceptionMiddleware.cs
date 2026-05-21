using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;

namespace SubscriptionSystem.WebUI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Sistem arızası nedeniyle işlem gerçekleşmedi! Hata Mesajı: {Message} | Path: {Path}", 
                    ex.Message, context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            string errorMessage = "Sistemde bir arıza oluştu. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin.";

            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var jsonResponse = JsonSerializer.Serialize(new { Message = errorMessage });
                return context.Response.WriteAsync(jsonResponse);
            }

            context.Response.Redirect($"/Home/Error?message={HttpUtility.UrlEncode(errorMessage)}");
            return Task.CompletedTask;
        }
    }
}