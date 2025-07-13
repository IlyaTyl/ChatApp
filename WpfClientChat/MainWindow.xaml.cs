using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WpfClientChat
{
    public partial class MainWindow : Window
    {
        HubConnection connection;
        DateTime dtNow = DateTime.UtcNow;
        private readonly string username;

        private ScrollViewer? chatScrollViewer;
        private bool userAtBottom = true;

        private ChatDto? currentChat = null;

        public MainWindow(string userName)
        {
            InitializeComponent();
            username = userName;

            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7226/chat")
                .WithAutomaticReconnect()
                .Build();

            connection.On<Message>("Receive", (chatMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (currentChat != null && chatMessage.ChatId == currentChat.Id)
                    {
                        chatbox.Items.Add(chatMessage);

                        if (userAtBottom)
                            chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                    }
                });
            });

            connection.On<List<UserDto>>("ReceiveSearchResults", users =>
            {
                Dispatcher.Invoke(() =>
                {
                    searchResultsListBox.ItemsSource = users;
                });
            });
        }

        public async Task<bool> StartConnectionAsync()
        {
            try
            {
                await connection.StartAsync();
                sendBtn.IsEnabled = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                return false;
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

        private async void SelectChat(ChatDto chat)
        {
            try
            {
                currentChat = chat;
                chatbox.Items.Clear();

                // Загружаем историю по ChatId
                await connection.InvokeAsync("JoinChat", chat.Id);
                var messages = await connection.InvokeAsync<List<Message>>("GetMessagesByChatId", chat.Id);

                foreach (var msg in messages.OrderBy(m => m.SentAt))
                {
                    chatbox.Items.Add(msg);
                }

                if (chatbox.Items.Count > 0)
                {
                    chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = messageTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text) && currentChat != null)
                {
                    await connection.InvokeAsync("SendMessageToChat", currentChat.Id, username, text);
                    messageTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    if (currentChat != null)
                    {
                        await connection.InvokeAsync("SendMessageToChat", currentChat.Id, "", $"Пользователь {username} выходит из чата");
                    }
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




        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string query = searchTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    await connection.InvokeAsync("SearchUsers", query);
                }
                else
                {
                    searchResultsListBox.ItemsSource = null;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void АddPrivateChat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDto targetUser)
            {
                try
                {
                    var chat = await connection.InvokeAsync<ChatDto>("CreatePrivateChat", username, targetUser.UserName);

                    if (chat != null)
                    {
                        var exists = privateChatsListBox.Items.Cast<ChatDto>().Any(c => c.Id == chat.Id);

                        if(!exists)
                        {
                            privateChatsListBox.Items.Add(chat);
                        }

                        // Автоматически переходим в чат
                        SelectChat(chat);
                        searchTextBox.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании чата: {ex.Message}");
                } 
            }
        }

        private void PrivateChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(privateChatsListBox.SelectedItem is ChatDto selectedChat)
            {
                SelectChat(selectedChat);
            }
        }
    }
}