using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models.Entity
{
    public enum MessageType
    {
        Text,
        Image,
        Video,
        Download,
        File
    }
    public class Message
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? Text { get; set; }
        public string? FilePath { get; set; }
        public string? OriginalFileName { get; set; }
        public MessageType Type { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();

        [NotMapped]
        public string? LocalPath { get; set; }
    }
}
