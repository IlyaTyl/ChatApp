using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
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

namespace WpfClientChat
{
    public class PreviewImage
    {
        public string Path { get; set; } = null!;
        public string? Caption { get; set; }
    }

    public partial class ImagePreviewWindow : Window
    {
        private HubConnection connection;
        private ChatDto currentChat;
        private string currentUsername;
        public ObservableCollection<PreviewImage> Images { get; set; }

        public ImagePreviewWindow(IEnumerable<string> filePaths, HubConnection hubConnection, ChatDto chat, string username)
        {
            InitializeComponent();
            Images = new ObservableCollection<PreviewImage>(
                filePaths.Select(p => new PreviewImage { Path = p })
            );
            DataContext = this;

            connection = hubConnection;
            currentChat = chat;
            currentUsername = username;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var img in Images)
            {
                var fileInfo = new FileInfo(img.Path);

                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Размер файла не должен превышать 10 МБ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                byte[] fileBytes = File.ReadAllBytes(img.Path);

                string imagePath = await connection.InvokeAsync<string>("UploadImage", fileBytes, fileInfo.Name);

                await connection.InvokeAsync("SendMessageToChat", currentChat.Id, currentUsername, img.Caption, imagePath);
            }
            Close();
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn && btn.Tag is PreviewImage img && Images.Contains(img))
                Images.Remove(img);
        }

        private void CancelSendButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
