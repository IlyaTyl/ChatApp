using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public ICollection<ChatUser> ChatUsers { get; set; } = new List<ChatUser>();
        public ICollection<FriendRequest> SentFriendRequests {  get; set; } = new List<FriendRequest>();
        public ICollection<FriendRequest> ReceivedFriendRequests {  get; set; } = new List<FriendRequest>();
        public ICollection<Friend> Friends { get; set; } = new List<Friend>();
        public ICollection<Friend> FriendOf { get; set; } = new List<Friend>();
    }
}
