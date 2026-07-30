using Microsoft.AspNetCore.SignalR.Client;
using MS.WindowsAPICodePack.Internal;
using SignalRAppChat.Shared.Models.Dto;
using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using WpfClientChat.Helper;
using WpfClientChat.Service;
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

        public ObservableCollection<ChatDto> PrivateChats { get; set; } = new();
        public ObservableCollection<ChatDto> GroupChats { get; set; } = new();

        private ObservableCollection<PreviewFile> downloadFiles;
        private readonly string downloadsFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyChatApp", "DownloadsFiles");

        // Отображаемая коллекция для поиска
        private ICollectionView downloadsView;

        public MainWindow(string userName)
        {
            InitializeComponent();
            username = userName;
            privateChatsListBox.ItemsSource = PrivateChats;
            groupChatsListBox.ItemsSource = GroupChats;

            LoadDownloads();

            downloadsView = CollectionViewSource.GetDefaultView(downloadFiles);
            downloadsListBox.ItemsSource = downloadFiles;

            connection = new HubConnectionBuilder()
                .WithUrl($"https://localhost:7226/chat?username={username}")
                .WithAutomaticReconnect()
                .Build();

            connection.On<int>("ReceiveHighlightChat", chatId =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (currentChat == null || chatId != currentChat.Id)
                    {
                        HighlightChat(chatId);
                    }
                });
            });

            connection.On<ChatDto>("ReceiveNewGroupChat", async chat =>
            {
                Dispatcher.Invoke(() =>
                {
                    GroupChats.Add(chat);
                });
            });

            connection.On<ChatDto>("ReceiveNewPrivateChat", async chat =>
            {
                Dispatcher.Invoke(() =>
                {   
                    PrivateChats.Add(chat);
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

            connection.On<UserDto>("FriendRequestCancelled", user =>
            {
                Dispatcher.Invoke(() =>
                {
                    RemoveFromRequestList(user);
                });
            });

            connection.On<int>("ChatHasBeenDeleted", (deletedChatId) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var privateChat = PrivateChats
                        .OfType<ChatDto>()
                        .FirstOrDefault(c => c.Id == deletedChatId);
                    if (privateChat != null)
                        PrivateChats.Remove(privateChat);

                    var groupChat = GroupChats
                        .OfType<ChatDto>()
                        .FirstOrDefault(c => c.Id == deletedChatId);
                    if (groupChat != null)
                        GroupChats.Remove(groupChat);

                    ChatContentControl.Content = null;
                });
            });

            connection.On<UserDto>("FriendRemoved", removedUser =>
            {
                Dispatcher.Invoke(() =>
                {
                    var userToRemove = friendsListBox.Items.Cast<UserDto>().FirstOrDefault(u => u.Id == removedUser.Id);
                    if(userToRemove != null)
                    {
                        friendsListBox.Items.Remove(userToRemove);
                    }
                });
            });
        }

        //Вывод загруженных файлов

        private void LoadDownloads()
        {
            if (!Directory.Exists(downloadsFolder))
            {
                Directory.CreateDirectory(downloadsFolder);
            }

            var files = Directory.GetFiles(downloadsFolder)
            .Select(filePath => new PreviewFile
            {
                Path = filePath,
                Type = FileTypeHelper.GetMessageTypeByExtension(System.IO.Path.GetExtension(filePath))
            });

            downloadFiles = new ObservableCollection<PreviewFile>(files);
        }

        public async Task<bool> StartConnectionAsync()
        {
            try
            {
                await connection.StartAsync();

                // Подключаемся ко всем чат-группам и выводим их
                var chats = await connection.InvokeAsync<List<ChatDto>>("GetUserChats", username);
                var chatIds = chats.Select(c => c.Id).ToList();
                await connection.InvokeAsync("JoinAllChats", chatIds);

                foreach (var chat in chats)
                {
                    if (chat.IsGroup)
                    {
                        GroupChats.Add(chat);
                    }
                    else
                    {
                        PrivateChats.Add(chat);
                    }
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
                await connection.InvokeAsync("CancelFriendRequest", username, user.UserName);
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
            var chat = PrivateChats.FirstOrDefault(c => c.Id == chatId);

            if (chat == null)
                chat = GroupChats.FirstOrDefault(c => c.Id == chatId);

            if(chat != null)
            {
                chat.UnreadCount++;
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
        private void SelectChat(ChatDto chat)
        {
            try
            {
                currentChat = chat;

                //Ставим значение пропущенных сообщений на 0
                chat.UnreadCount = 0;
                //Открываем представление ChatView с выбранным чатом
                var chatView = new ChatView(connection, chat, username);
                ChatContentControl.Content = chatView;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }

        //Закрытие окна
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Будущая реализация
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
                        var exists = PrivateChats.Cast<ChatDto>().Any(c => c.Id == chat.Id);

                        if(!exists)
                        {
                            PrivateChats.Add(chat);
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
        //Обработчик нажатия кнопки добавления группового чата
        private async void AddGroupChat_Click(object sender, RoutedEventArgs e)
        {
            List<string> friendUserNames = new List<string>();
            var friendUsers = await connection.InvokeAsync<List<UserDto>>("GetFriends", username);

            foreach (var user in friendUsers)
            {
                friendUserNames.Add(user.UserName);
            }

            var createGroupChatWindow = new CreateGroupChatWindow(username, friendUserNames, connection);
            bool? result = createGroupChatWindow.ShowDialog();

            if (result == true && createGroupChatWindow.CreatedChat != null)
            {
                GroupChats.Add(createGroupChatWindow.CreatedChat);
                SelectChat(createGroupChatWindow.CreatedChat);
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
                groupChatsListBox.SelectedItem = null;
                SelectChat(selectedChat);
            }
        }

        //Обработчик нажатия кнопки удаления друга
        private async void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem menuItem && menuItem.DataContext is UserDto targetUser)
            {
                var result = MessageBox.Show($"Удалить {targetUser.UserName} из друзей?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = await connection.InvokeAsync<bool>("RemoveFriend", username, targetUser.UserName);
                        if (success)
                        {
                            friendsListBox.Items.Remove(targetUser);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить друга.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления друга: {ex.Message}");
                    }
                }
            }
        }

        //Обработчик выбора группового чата
        private void GroupChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(groupChatsListBox.SelectedItem is ChatDto selectedChat)
            {
                privateChatsListBox.SelectedItem = null;
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

        private void SearchDownloadTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter(searchDownloadTextBox.Text);
        }

        private void SearchDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySearchFilter(searchDownloadTextBox.Text);
        }

        //Открытие скачанного файла двойным нажатием
        private void DownloadsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (downloadsListBox.SelectedItem is PreviewFile downloadFile)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = downloadFile.Path,
                        UseShellExecute = true
                    });
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл: {ex.Message}");
                }
            }
        }

        //Удаление скаченных файлов
        private void DeleteDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn && btn.Tag is PreviewFile downloadFile)
            {
                var result = MessageBox.Show($"Удалить файл \"{downloadFile.FileName}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if(File.Exists(downloadFile.Path))
                            File.Delete(downloadFile.Path);

                        downloadFiles.Remove(downloadFile);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                    }
                }
            }
        }

        private void RefreshDownloadList()
        {
            downloadFiles.Clear();
            if (Directory.Exists(downloadsFolder))
            {
                var files = Directory.GetFiles(downloadsFolder);
                foreach(var filePath in files)
                {
                    downloadFiles.Add(new PreviewFile
                    {
                        Path = filePath,
                        Type = FileTypeHelper.GetMessageTypeByExtension(System.IO.Path.GetExtension(filePath))
                    });
                }
            }

            ApplySearchFilter(searchDownloadTextBox.Text);
        }

        //Фильтрация для поиска
        private void ApplySearchFilter(string searchText)
        {
            if (downloadsView == null) return;

            downloadsView.Filter = item =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return true;

                if (item is PreviewFile file)
                    return file.FileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                return false;
            };

            downloadsView.Refresh();
        }

        //Обновление списка загрузок при переключении
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.Source is TabControl tabControl && tabControl.SelectedItem is TabItem tabItem)
            {
                if(tabItem.Name == "DownloadsTabItem")
                {
                    RefreshDownloadList();
                }
            }
        }
    }
}