using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRAppChat.Shared.Models
{
    public class ChatDto : INotifyPropertyChanged
    {
        private int unreadCount;

        public int Id { get; set; }
        public string? Name { get; set; } = null;
        public bool IsGroup { get; set; }
        public List<UserDto> Users { get; set; } = new();
        public int UnreadCount
        {
            get => unreadCount;
            set
            {
                if(unreadCount != value)
                {
                    unreadCount = value;
                    OnPropertyChanged(nameof(UnreadCount));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
