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

        public async Task<List<Message>> GetMessagesByChatId(int chatId)
        {
            return await context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task JoinChat(int chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        public async Task SendMessageToChat(int chatId, string senderUsername, string text)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == senderUsername);
            if (user == null) return;

            var chatMessage = new Message
            {
                ChatId = chatId,
                UserId = user.Id,
                UserName = senderUsername,
                Text = text
            };

            context.Messages.Add(chatMessage);
            await context.SaveChangesAsync();

            await this.Clients.Group($"chat_{chatId}").SendAsync("Receive", chatMessage);
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

        public async Task<ChatDto?> CreatePrivateChat(string currentUserName, string targetUserName)
        {
            var user1 = await context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            var user2 = await context.Users.FirstOrDefaultAsync(u => u.UserName == targetUserName);

            if (user1 == null || user2 == null || user1.Id == user2.Id)
                return null;


            //
            var existingChat = await context.Chats
                .Include(c => c.ChatUsers).ThenInclude(cu => cu.User)
                .Where(c => !c.IsGroup &&
                    c.ChatUsers.Count == 2 &&
                    c.ChatUsers.Any(cu => cu.UserId == user1.Id) &&
                    c.ChatUsers.Any(cu => cu.UserId == user2.Id))
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                return new ChatDto
                {
                    Id = existingChat.Id,
                    Name = existingChat.Name,
                    IsGroup = existingChat.IsGroup,
                    Users = existingChat.ChatUsers
                                .Select(cu => new UserDto
                                {
                                    Id = cu.User.Id,
                                    UserName = cu.User.UserName
                                }).ToList()
                };
            }

            var newChat = new Chat { IsGroup = false , Name = $"{currentUserName} и {targetUserName}" };
            newChat.ChatUsers.Add(new ChatUser { UserId = user1.Id });
            newChat.ChatUsers.Add(new ChatUser { UserId = user2.Id });

            context.Chats.Add(newChat);
            await context.SaveChangesAsync();

            return new ChatDto
            {
                Id = newChat.Id,
                Name = newChat.Name,
                IsGroup = newChat.IsGroup,
                Users = new List<UserDto>
                {
                    new UserDto { Id = user1.Id, UserName = user1.UserName },
                    new UserDto { Id = user2.Id, UserName = user2.UserName }
                }
            };
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