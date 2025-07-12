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
        public string UserName { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
