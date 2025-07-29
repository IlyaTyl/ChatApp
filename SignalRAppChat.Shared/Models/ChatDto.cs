using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class ChatDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } = null;
        public bool IsGroup { get; set; }
        public List<UserDto> Users { get; set; } = new();
    }
}
