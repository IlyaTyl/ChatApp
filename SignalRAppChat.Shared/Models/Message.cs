using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Text { get; set; }

        public string? ImagePath { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();

    }
}
