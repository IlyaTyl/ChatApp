using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRAppChat.Data;
using SignalRAppChat.Shared.Models;

namespace SignalRAppChat
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext context;

        public ChatHub(ApplicationDbContext appDbContext)
        {
            context = appDbContext;
        }

        public async Task<List<Message>> GetHistory()
        {
            return await context.Messages
                .OrderByDescending(m => m.Id)
                .Take(50)
                .ToListAsync();
        }

        public async Task Send(string username, string message)
        {
            var chatMessage = new Message
            {
                UserName = username,
                Text = message
            };

            context.Messages.Add(chatMessage);
            await context.SaveChangesAsync();

            await this.Clients.All.SendAsync("Receive", chatMessage);
        }

        public async Task SearchUsers(string search)
        {
            var users = await context.Users
                .Where(u => u.UserName.StartsWith(search))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("ReceiveSearchResults", users);
        }

        public async Task<bool> Register(string username, string password)
        {
            if (context.Users.Any(u => u.UserName == username))
                return false;

            var user = new User { UserName = username, Password = password };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Login(string username, string password)
        {
            return context.Users.Any(u => u.UserName == username && u.Password == password);
        }
    }
}