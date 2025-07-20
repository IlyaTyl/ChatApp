using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class SelectableFriendToGroupChat
    {
        public string UserName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
