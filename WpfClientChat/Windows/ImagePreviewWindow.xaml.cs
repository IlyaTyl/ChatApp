using Microsoft.AspNetCore.SignalR.Client;
using SignalRAppChat.Shared.Models.Dto;
using SignalRAppChat.Shared.Models.Entity;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfClientChat.Helper;
using WpfClientChat.Service;

namespace WpfClientChat
{
    public class PreviewFile
    {
        public string Path { get; set; } = null!;
        public string? Caption { get; set; }
        public MessageType Type { get; set; }
        public BitmapSource? PreviewImage =>
            Type == MessageType.Video ? VideoPreviewHelper.GetThumbnail(Path) : null;
        public string FileName => System.IO.Path.GetFileName(Path);
    }

    public partial class ImagePreviewWindow : Window
    {
        private HubConnection connection;
        private ChatDto currentChat;
        private string currentUsername;
        private FileCacheService fileCacheService = new FileCacheService();
        public ObservableCollection<PreviewFile> Files { get; set; }

        public ImagePreviewWindow(IEnumerable<string> filePaths, HubConnection hubConnection, ChatDto chat, string username)
        {
            InitializeComponent();
            Files = new ObservableCollection<PreviewFile>(
                filePaths.Select(p => new PreviewFile 
                { 
                    Path = p,
                    Type = FileTypeHelper.GetMessageTypeByExtension(System.IO.Path.GetExtension(p))
                })
            );
            DataContext = this;

            connection = hubConnection;
            currentChat = chat;
            currentUsername = username;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files)
            {
                var fileInfo = new FileInfo(file.Path);

                if (fileInfo.Length > 50 * 1024 * 1024)
                {
                    MessageBox.Show("Размер файла не должен превышать 50 МБ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                byte[] fileBytes = File.ReadAllBytes(file.Path);

                string fileUrl = await connection.InvokeAsync<string>("UploadFile", fileBytes, fileInfo.Name);

                await fileCacheService.SaveToCacheAsync(Path.GetFileName(fileUrl), fileBytes);

                var type = FileTypeHelper.GetMessageTypeByExtension(fileInfo.Extension);

                await connection.InvokeAsync("SendMessageToChat", currentChat.Id, currentUsername, file.Caption, fileUrl, fileInfo.Name, type);
            }
            Close();
        }

        private void CancelSendButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PreviewFile file && Files.Contains(file))
                Files.Remove(file);
        }
    }
}
