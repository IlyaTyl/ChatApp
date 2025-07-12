using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfClientChat
{
    public partial class MainWindow : Window
    {
        HubConnection connection;
        DateTime dtNow = DateTime.UtcNow;
        private readonly string username;

        private ScrollViewer? chatScrollViewer;
        private bool userAtBottom = true;

        public MainWindow(string userName)
        {
            InitializeComponent();
            username = userName;

            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7226/chat")
                .WithAutomaticReconnect()
                .Build();

            connection.On<string, string>("Receive", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var newMessage = $"{dtNow:HH:mm} {user}: {message}";
                    chatbox.Items.Add(newMessage);

                    if(userAtBottom)
                        chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                });
            });
        }

        public async Task<bool> StartConnectionAsync()
        {
            try
            {
                await connection.StartAsync();
                await LoadHistoryAsync();
                chatbox.Items.Add("Вы вошли в чат");
                sendBtn.IsEnabled = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var messages = await connection.InvokeAsync<List<Message>>("GetHistory");

                foreach (var msg in messages.OrderBy(m => m.SentAt))
                {
                    chatbox.Items.Add($"{msg.SentAt:HH:mm} {msg.UserName}: {msg.Text}");
                }

                if (chatbox.Items.Count > 0)
                {
                    chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                chatbox.Items.Add("Ошибка при загрузке истории: " + ex.Message);
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if(depObj is ScrollViewer) return (ScrollViewer)depObj;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null!;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = messageTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    await connection.InvokeAsync("Send", username, text);
                    messageTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                chatbox.Items.Add($"Ошибка отправки: {ex.Message}");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    await connection.InvokeAsync("Send", "", $"Пользователь {username} выходит из чата");
                    await connection.StopAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отключении: {ex.Message}");
            }
        }

        private void chatbox_Loaded(object sender, RoutedEventArgs e)
        {
            chatScrollViewer = GetScrollViewer(chatbox);
        }

        private void chatbox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (chatScrollViewer == null) return;

            userAtBottom = chatScrollViewer.VerticalOffset >= chatScrollViewer.ScrollableHeight - 1;
        }
    }
}