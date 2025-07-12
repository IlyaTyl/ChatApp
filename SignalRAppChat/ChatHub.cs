using Microsoft.AspNetCore.SignalR;

namespace SignalRAppChat
{
    public class ChatHub : Hub
    {
        public async Task Send(string username, string message)
        {
            await this.Clients.All.SendAsync("Receive", username, message);
        }
    }
}