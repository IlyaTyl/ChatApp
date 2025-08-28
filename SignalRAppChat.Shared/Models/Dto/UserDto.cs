using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models.Dto
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
    }
}
