using SignalRAppChat.Shared.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models.Dto
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string UserName { get; set; } = null!;
        public string? Text { get; set; }
        public string? FilePath { get; set; }
        public string? OriginalFileName { get; set; }
        public MessageType Type { get; set; }
        public DateTime SentAt { get; set; }

        public string? LocalPath { get; set; }
    }
}
