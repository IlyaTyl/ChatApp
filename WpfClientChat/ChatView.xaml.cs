using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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
        private DispatcherTimer typingCleanupTimer;

        public ChatView(HubConnection connection, ChatDto chat, string username)
        {
            this.connection = connection;
            this.username = username;
            currentChat = chat;
            InitializeComponent();
            SetupTypingHandler();

            connection.On<Message>("Receive", (chatMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (chatMessage.ChatId == currentChat.Id)
                    {
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

        //Загрузка ChatView
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                chatbox.Items.Clear();
                chatTitle.Text = currentChat.Name;

                // Загружаем историю по ChatId
                await connection.InvokeAsync("JoinChat", currentChat.Id);
                var messages = await connection.InvokeAsync<List<Message>>("GetMessagesByChatId", currentChat.Id);

                foreach (var msg in messages.OrderBy(m => m.SentAt))
                {
                    chatbox.Items.Add(msg);
                }

                if (chatbox.Items.Count > 0)
                {
                    chatbox.ScrollIntoView(chatbox.Items[chatbox.Items.Count - 1]);
                }

                if(currentChat.IsGroup)
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
                    await connection.InvokeAsync("SendMessageToChat", currentChat.Id, username, text);
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
    }


}
