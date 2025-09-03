using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models.Dto;
using SignalRAppChat.Shared.Models.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;
using WpfClientChat.Service;

namespace WpfClientChat
{
    public partial class ChatView : UserControl
    {
        HubConnection connection;
        DateTime dtNow = DateTime.UtcNow;
        private readonly string username;

        private ScrollViewer? chatScrollViewer;
        private bool userAtBottom = true;

        private ChatDto currentChat;
        private bool isAdmin = false;

        private Dictionary<string, DateTime> typingUsers = new();
        private DispatcherTimer? typingCleanupTimer;

        private readonly FileCacheService fileCacheService = new FileCacheService();

        public ChatView(HubConnection connection, ChatDto chat, string username)
        {
            this.connection = connection;
            this.username = username;
            currentChat = chat;
            InitializeComponent();
            SetupTypingHandler();

            connection.On<MessageDto>("Receive", (chatMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (chatMessage.ChatId == currentChat.Id)
                    {
                        if (!string.IsNullOrEmpty(chatMessage.OriginalFileName) && !string.IsNullOrEmpty(chatMessage.FilePath))
                        {
                            if (fileCacheService.TryGetCachedFile(chatMessage.OriginalFileName, out string cachedPath))
                            {
                                chatMessage.LocalPath = cachedPath;
                            }
                            else
                            {
                                chatMessage.LocalPath = chatMessage.FilePath;
                            }
                        }

                        chatbox.Items.Add(chatMessage);

                        if (userAtBottom)
                            chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                    }
                });
            });

            connection.On<int>("ChatHistoryCleared", (chatId) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (currentChat != null && currentChat.Id == chatId)
                    {
                        chatbox.Items.Clear();
                    }
                });
            });
        }

        //Загрузка ChatView
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                chatbox.Items.Clear();
                chatTitle.Text = currentChat.Name;

                //Уведомляем сервер, что сообщения прочтены
                await connection.InvokeAsync("MarkMessagesAsRead", currentChat.Id, username);

                //Загружаем историю по ChatId
                await connection.InvokeAsync("JoinChat", currentChat.Id);
                var messages = await connection.InvokeAsync<List<Message>>("GetMessagesByChatId", currentChat.Id);

                foreach (var msg in messages.OrderBy(m => m.SentAt))
                {
                    if(!string.IsNullOrEmpty(msg.FilePath) && !string.IsNullOrEmpty(msg.OriginalFileName))
                    {
                        //Проверка, есть ли файл в кэше
                        if(fileCacheService.TryGetCachedFile(msg.OriginalFileName, out string cachedFile))
                        {
                            msg.LocalPath = cachedFile;
                        }
                        else
                        {
                            msg.LocalPath = msg.FilePath;
                        }
                    }

                    chatbox.Items.Add(msg);
                }

                if (chatbox.Items.Count > 0)
                {
                    chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                }

                if (currentChat.IsGroup)
                {
                    isAdmin = await connection.InvokeAsync<bool>("IsUserAdmin", currentChat.Id, username);
                }
                else
                {
                    isAdmin = true;
                }

                deleteChatMenuItem.Visibility = isAdmin || !currentChat.IsGroup ? Visibility.Visible : Visibility.Collapsed;
                clearChatHistoryMenuItem.Visibility = isAdmin || !currentChat.IsGroup ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }

        //Создаем таймер и подписываемся на событие печати пользователя
        private void SetupTypingHandler()
        {
            typingCleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            typingCleanupTimer.Tick += TypingCleanupTimer_Tick;
            typingCleanupTimer.Start();

            connection.On<int, string>("UserTyping", (chatId, typingUser) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (chatId != currentChat.Id || typingUser == username)
                        return;

                    typingUsers[typingUser] = DateTime.UtcNow;
                    UpdateTypingIndicator();
                });
            });
        }

        //Метод убирающий непечатающих пользователей
        private void TypingCleanupTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            var expired = typingUsers
                .Where(kvp => (now - kvp.Value).TotalSeconds > 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var user in expired)
            {
                typingUsers.Remove(user);
            }

            UpdateTypingIndicator();
        }

        //Метод обновления индикатора печати сообщения
        private void UpdateTypingIndicator()
        {
            if (typingUsers.Count == 0)
            {
                typingIndicator.Visibility = Visibility.Collapsed;
                typingIndicator.Content = "";
            }

            else if (typingUsers.Count == 1)
            {
                var name = typingUsers.Keys.First();
                typingIndicator.Content = $"{name} пишет...";
                typingIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                typingIndicator.Content = "Несколько человек пишут...";
                typingIndicator.Visibility = Visibility.Visible;
            }
        }

        //Выводит на каком сообщении находится ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer) return (ScrollViewer)depObj;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null!;
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

        //Отправка сообщения в чат
        private async void SendMessageToChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = messageTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(text) && currentChat != null)
                {
                    await connection.InvokeAsync("SendMessageToChat", currentChat.Id, username, text, null, null, MessageType.Text);
                    messageTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки: {ex.Message}");
            }
        }

        //Нажатие на кнопку контекстного меню
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn?.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        //Нажатие на кнопку удаления чата
        private async void DeleteChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить чат?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await connection.InvokeAsync("DeleteChat", currentChat.Id);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении чата: {ex.Message}");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка удаления чата: {ex.Message}");
            }
        }

        //Нажатие на кнопку удаления истории чата
        private async void ClearChatHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить всю историю сообщений этого чата?",
                    "Очистка истории чата",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await connection.InvokeAsync("ClearChatHistory", currentChat.Id);
                        chatbox.Items.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении истории: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка {ex.Message}");
            }
        }

        //Обработчик изменения текста
        private async void messageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(messageTextBox.Text))
            {
                await connection.InvokeAsync("Typing", currentChat.Id, username);
            }
        }

        //Обработчик нажатие на кнопку прикрепления файла
        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn?.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
        //Обработчик нажатие на кнопку отправки картинки
        private void SendImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images|*.jpg;*.png;*.gif;*.bmp",
                    Multiselect = true
                };

                if (dlg.ShowDialog() == true)
                {
                    var previewWindow = new ImagePreviewWindow(dlg.FileNames, connection, currentChat, username);
                    previewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки изображения: {ex.Message}");
            }
        }

        //Обработчик нажатие на кнопку отправки любого документа
        private void SendDocButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "All files|*.*",
                    Multiselect = true
                };

                if (dlg.ShowDialog() == true)
                {
                    var previewWindow = new ImagePreviewWindow(dlg.FileNames, connection, currentChat, username);
                    previewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки документа: {ex.Message}");
            }
        }

        //Обработчик нажатие на кнопку отправки видео
        private void SendVideoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Video|*.mp4;*.avi;*.mov;*.wmv;*.mkv",
                    Multiselect = true
                };

                if (dlg.ShowDialog() == true)
                {
                    var previewWindow = new ImagePreviewWindow(dlg.FileNames, connection, currentChat, username);
                    previewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки видео: {ex.Message}");
            }
        }

        //Сохранение файла в кэш
        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn && btn.Tag is Message msg)
            {
                try
                {
                    if (string.IsNullOrEmpty(msg.FilePath) || string.IsNullOrEmpty(msg.OriginalFileName))
                    {
                        return;
                    }

                    //Если нет в кэше, то качаем и используем из кэша
                    if(!fileCacheService.TryGetCachedFile(msg.OriginalFileName, out string cachedPath))
                    {
                        cachedPath = await fileCacheService.GetOrDownloadFileAsync(System.IO.Path.GetFileName(msg.FilePath), msg.FilePath);
                        msg.LocalPath = cachedPath;
                    }
                    
                    //Будущая реализация открытия самого файла
                    //
                    // ...
                    //
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия файла: {ex.Message}");
                }
            }
        }
    }


}
