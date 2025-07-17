using Microsoft.AspNetCore.SignalR;

namespace SignalRAppChat
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Получаем имя пользователя из строки запроса
            return connection.GetHttpContext()?.Request.Query["username"];
        }
    }
}
