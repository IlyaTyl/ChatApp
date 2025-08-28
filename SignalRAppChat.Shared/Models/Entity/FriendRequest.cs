using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models.Entity
{
    public class FriendRequest
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;
        public int ReceiverId { get; set; }
        public User Receiver { get; set; } = null!;
        public bool IsAccepted { get; set; } = false;
    }
}
