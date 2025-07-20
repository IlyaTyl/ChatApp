using Azure.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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

        //Вывод сообщений по чат-id
        public async Task<List<Message>> GetMessagesByChatId(int chatId)
        {
            return await context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        //Вывод чатов у пользователя
        public async Task<List<ChatDto>> GetUserChats(string userName)
        {
            var user = await context.Users
                .Include(u => u.ChatUsers)
                .ThenInclude(cu => cu.Chat)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null) return new();

            var chats = user.ChatUsers
                .Select(cu => cu.Chat)
                .Distinct()
                .ToList();

            return chats.Select(c => new ChatDto
            {
                Id = c.Id,
                Name = c.Name,
                IsGroup = c.IsGroup,
                Users = c.ChatUsers.Select(cu => new UserDto
                {
                    Id = cu.User.Id,
                    UserName = cu.User.UserName
                }).ToList()
            }).ToList();
        }

        //Вывод друзей
        public async Task<List<UserDto>> GetFriends(string userName)
        {
            var user = await context.Users
                .Include(u => u.Friends)
                .ThenInclude(f => f.FriendUser)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null) return new();

            return user.Friends
                .Select(f => new UserDto { Id = f.FriendUser.Id, UserName = f.FriendUser.UserName})
                .ToList();
        }

        //Вывод заявок в друзья
        public async Task<List<UserDto>> GetFriendRequestsReceivers(string userName)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null) return new();

            var requests = await context.Set<FriendRequest>()
                .Include(fr => fr.Sender)
                .Where(fr => fr.ReceiverId == user.Id && !fr.IsAccepted)
                .ToListAsync();

            return requests.Select(fr => new UserDto { Id = fr.Sender.Id, UserName = fr.Sender.UserName }).ToList();
        }

        public async Task<List<UserDto>> GetFriendRequestsSenders(string userName)
        {
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null) return new();

            var requests = await context.Set<FriendRequest>()
                .Include(fr => fr.Receiver)
                .Where(fr => fr.SenderId == user.Id && !fr.IsAccepted)
                .ToListAsync();

            return requests.Select(fr => new UserDto { Id = fr.Receiver.Id, UserName = fr.Receiver.UserName }).ToList();
        }

        //Присоединение к чату и активным чатам
        public async Task JoinChat(int chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }

        public async Task JoinAllChats(List<int> chatIds)
        {
            foreach (var chatId in chatIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            }
        }

        //Отправка сообщения
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

        //Отправка запроса в друзья
        public async Task SendFriendRequest(string fromUsername, string toUsername)
        {
            try
            {
                var fromUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == fromUsername);
                var toUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == toUsername);

                if (fromUser == null || toUser == null || fromUser.Id == toUser.Id)
                    return;

                bool alreadyRequested = await context.Set<FriendRequest>().AnyAsync(fr =>
                (fr.SenderId == fromUser.Id && fr.Receiver.Id == toUser.Id) || (fr.SenderId == toUser.Id && fr.Receiver.Id == fromUser.Id));

                if (alreadyRequested)
                    return;

                var request = new FriendRequest
                {
                    Sender = fromUser,
                    Receiver = toUser
                };

                context.Add(request);
                await context.SaveChangesAsync();

                await Clients.User(toUsername).SendAsync("FriendRequestReceived", new UserDto { Id = fromUser.Id, UserName = fromUser.UserName });
                await Clients.User(fromUsername).SendAsync("FriendRequestSent", new UserDto { Id = toUser.Id, UserName = toUser.UserName });
            }
            catch (Exception)
            {
                throw new HubException("Ошибка при отправке заявки в друзья.");
            }
        }

        //Принятие запроса в друзья
        public async Task AcceptFriendRequest(string currentUserName, string requesterName)
        {
            var currentUser = await context.Users.Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.UserName == currentUserName);
            var requester = await context.Users.Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.UserName == requesterName);

            if (currentUser == null || requester == null)
                return;

            var request = await context.Set<FriendRequest>()
                .FirstOrDefaultAsync(fr => fr.SenderId == requester.Id && fr.ReceiverId == currentUser.Id);

            if (request == null) 
                return;

            request.IsAccepted = true;

            context.Add(new Friend { UserId = currentUser.Id, FriendUserId = requester.Id });
            context.Add(new Friend { UserId = requester.Id, FriendUserId = currentUser.Id });

            await context.SaveChangesAsync();

            await Clients.User(currentUserName).SendAsync("FriendRequestAccepted", new UserDto { Id = requester.Id, UserName = requester.UserName });
            await Clients.User(requesterName).SendAsync("FriendRequestAccepted", new UserDto { Id = currentUser.Id, UserName = currentUser.UserName });
        }

        //Отмена заявки в друзья
        public async Task CancelFriendRequest(string currentUserName, string requesterOrReceiverName)
        {
            var currentUser = await context.Users.Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.UserName == currentUserName);
            var requesterOrReceiver = await context.Users.Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.UserName == requesterOrReceiverName);

            if (currentUser == null || requesterOrReceiver == null)
                return;

            var request = await context.Set<FriendRequest>()
                .FirstOrDefaultAsync(fr => fr.SenderId == requesterOrReceiver.Id && fr.ReceiverId == currentUser.Id && fr.IsAccepted == false);

            if (request == null) 
            {
                request = await context.Set<FriendRequest>()
                .FirstOrDefaultAsync(fr => fr.SenderId == currentUser.Id && fr.ReceiverId == requesterOrReceiver.Id && fr.IsAccepted == false);

                if (request == null)
                    return;
            }

            context.Set<FriendRequest>().Remove(request);

            await context.SaveChangesAsync();

            await Clients.User(currentUserName).SendAsync("FriendRequestCancelled", new UserDto { Id = requesterOrReceiver.Id, UserName = requesterOrReceiver.UserName });
            await Clients.User(requesterOrReceiverName).SendAsync("FriendRequestCancelled", new UserDto { Id = currentUser.Id, UserName = currentUser.UserName });
        }

        //Поиск пользователя
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

        //Создание приватного чата
        public async Task<ChatDto?> CreatePrivateChat(string currentUserName, string targetUserName)
        {
            var user1 = await context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            var user2 = await context.Users.FirstOrDefaultAsync(u => u.UserName == targetUserName);

            if (user1 == null || user2 == null || user1.Id == user2.Id)
                return null;

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

            var chatDto = new ChatDto
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

            await Clients.Users(targetUserName).SendAsync("ReceiveNewPrivateChat", chatDto);
            return chatDto;
        }

        //Создание группового чата
        public async Task<ChatDto?> CreateGroupChat(string creatorUserName, string groupName, List<string> participantUserNames)
        {
            var creator = await context.Users
                .Include(u => u.Friends)
                .Include(u => u.FriendOf)
                .FirstOrDefaultAsync(u => u.UserName == creatorUserName);

            if (creator == null)
                return null;

            var friendIds = creator.Friends.Select(f => f.FriendUserId)
                .Union(creator.FriendOf.Select(f => f.UserId))
                .ToHashSet();

            var usersToAdd = await context.Users
                .Where(u => participantUserNames.Contains(u.UserName))
                .ToListAsync();

            if(usersToAdd.Any(u => !friendIds.Contains(u.Id)))
            {
                return null;
            }

            var groupChat = new Chat
            {
                Name = groupName,
                IsGroup = true
            };

            groupChat.ChatUsers.Add(new ChatUser
            {
                UserId = creator.Id,
                IsAdmin = true
            });

            foreach (var user in usersToAdd)
            {
                groupChat.ChatUsers.Add(new ChatUser
                {
                    UserId = user.Id,
                    IsAdmin = false
                });
            }

            context.Chats.Add(groupChat);
            await context.SaveChangesAsync();

            var chatDto = new ChatDto
            {
                Id = groupChat.Id,
                Name = groupChat.Name,
                IsGroup = groupChat.IsGroup,
                Users = groupChat.ChatUsers
                    .Select(cu => new UserDto
                    {
                        Id = cu.User.Id,
                        UserName = cu.User.UserName
                    }).ToList()
            };

            var allParticipants = usersToAdd.Select(u => u.UserName).ToList();

            await Clients.Users(allParticipants).SendAsync("ReceiveNewGroupChat", chatDto);
            return chatDto;
        }

        //Аунтификация
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