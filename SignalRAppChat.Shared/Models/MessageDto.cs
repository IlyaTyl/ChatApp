using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public string UserName { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime SentAt { get; set; }
    }
}
