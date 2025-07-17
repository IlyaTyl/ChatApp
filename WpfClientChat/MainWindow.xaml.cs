using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
using System.Security.RightsManagement;
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
                .WithUrl($"https://localhost:7226/chat?username={username}")
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
                    else
                    {
                        HighlightChat(chatMessage.ChatId);
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

            connection.On<UserDto>("FriendRequestReceived", user => 
            {
                Dispatcher.Invoke(() => 
                {
                    AddRequestToList(user, isIncoming: true);
                });
            });

            connection.On<UserDto>("FriendRequestSent", user =>
            {
                Dispatcher.Invoke(() =>
                {
                    AddRequestToList(user, isIncoming: false);
                });
            });

            connection.On<UserDto>("FriendRequestAccepted", user =>
            {
                Dispatcher.Invoke(() =>
                {
                    friendsListBox.Items.Add(user);
                    RemoveFromRequestList(user);
                });
            });
        }

        public async Task<bool> StartConnectionAsync()
        {
            try
            {
                await connection.StartAsync();
                sendBtn.IsEnabled = true;

                // Подключаемся ко всем чат-группам и выводим их
                var chats = await connection.InvokeAsync<List<ChatDto>>("GetUserChats", username);
                var chatIds = chats.Select(c => c.Id).ToList();
                await connection.InvokeAsync("JoinAllChats", chatIds);

                foreach (var chat in chats)
                {
                    privateChatsListBox.Items.Add(chat);
                }

                //Вывод заявок в друзья
                var requestsReceiver = await connection.InvokeAsync<List<UserDto>>("GetFriendRequestsReceivers", username);
                foreach (var user in requestsReceiver)
                {
                    AddRequestToList(user, isIncoming: true);
                }
                var requestsSenders = await connection.InvokeAsync<List<UserDto>>("GetFriendRequestsSenders", username);
                foreach (var user in requestsSenders)
                {
                    AddRequestToList(user, isIncoming: false);
                }

                //Вывод всех друзей
                var friends = await connection.InvokeAsync<List<UserDto>>("GetFriends", username);
                foreach (var user in friends)
                {
                    friendsListBox.Items.Add(user);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }


        //Добавление и удаление заявок в друзья
        private void AddRequestToList(UserDto user, bool isIncoming)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock { Text = user.UserName });

            if (isIncoming)
            {
                var acceptBtn = new Button { Content = "Принять", Tag = user };
                acceptBtn.Click += AcceptFriend_Click;
                panel.Children.Add(acceptBtn);
            }

            var cancelBtn = new Button { Content = "Отмена", Tag = user };
            cancelBtn.Click += CancelRequest_Click;
            panel.Children.Add(cancelBtn);

            requestFriendsListBox.Items.Add(panel);
        }

        private void RemoveFromRequestList(UserDto user)
        {
            var itemsToRemove = requestFriendsListBox.Items.Cast<StackPanel>()
                .Where(p => (p.Children[0] as TextBlock)?.Text == user.UserName)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                requestFriendsListBox.Items.Remove(item);
            }
        }

        //Отмена и принятие заявок в друзья
        private async void CancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn && btn.Tag is UserDto user)
            {
                //Будущая реализация удаления заявки
                await connection.InvokeAsync("CancelFriendRequest");
            }
        }

        private async void AcceptFriend_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn && btn.Tag is UserDto user)
            {
                await connection.InvokeAsync("AcceptFriendRequest", username, user.UserName);
            }
        }

        //Счетчик новых сообщений в чате
        private void HighlightChat(int chatId)
        {
            foreach (ChatDto chat in privateChatsListBox.Items)
            {
                if(chat.Id == chatId)
                {
                    //Будущая реализация UnreadCount
                }
            }
        }

        //Выводит на каком сообщении находится ScrollViewer
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

        //Загрузка сообщений выбранного чата
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

        //Отправка сообщения в чат
        private async void SendMessageToChat_Click(object sender, RoutedEventArgs e)
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

        //Закрытие окна
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

        //Обработчик загрузки chatbox
        private void chatbox_Loaded(object sender, RoutedEventArgs e)
        {
            chatScrollViewer = GetScrollViewer(chatbox);
        }

        //Обработчик смены расположения Scroll в chatbox
        private void chatbox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (chatScrollViewer == null) return;

            userAtBottom = chatScrollViewer.VerticalOffset >= chatScrollViewer.ScrollableHeight - 1;
        }

        //Обработчик изменения текста в строке поиска
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

        //Обработчик нажатия кнопки добавления приватного чата
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

        //Обработчик нажатия кнопки добавления в друзья
        private async void АddToFriend_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDto targetUser)
            {
                try
                {
                    await connection.InvokeAsync("SendFriendRequest", username, targetUser.UserName);
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}");
                }
            }
        }

        //Обработчик выбора приватного чата
        private void PrivateChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(privateChatsListBox.SelectedItem is ChatDto selectedChat)
            {
                SelectChat(selectedChat);
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void FriendsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RequestFriendsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}