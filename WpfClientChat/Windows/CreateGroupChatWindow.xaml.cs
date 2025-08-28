using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
using SignalRAppChat.Shared.Models.Dto;

namespace WpfClientChat
{
    public partial class CreateGroupChatWindow : Window
    {
        private readonly HubConnection connection;
        private readonly string currentUserName;
        private List<SelectableFriendToGroupChat> friendList = new();
        public ChatDto? CreatedChat { get; private set; }

        public CreateGroupChatWindow(string currentUserName, List<string> friends, HubConnection connection)
        {
            InitializeComponent();
            this.currentUserName = currentUserName;
            this.connection = connection;

            friendList = friends.Select(f => new SelectableFriendToGroupChat()
            {
                UserName = f
            }).ToList();

            friendsListBox.ItemsSource = friendList;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string groupName = groupNameTextBox.Text.Trim();
            var selectedUsers = friendList.Where(f => f.IsSelected)
                .Select(f => f.UserName).ToList();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Введите название группы.");
                return;
            }

            if (selectedUsers.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного друга.");
                return;
            }

            CreatedChat = await connection.InvokeAsync<ChatDto>("CreateGroupChat", currentUserName, groupName, selectedUsers);

            if (CreatedChat != null)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Не удалось создать чат.");
            }
        }
    }
}
