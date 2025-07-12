using Microsoft.AspNetCore.SignalR.Client;
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
        public MainWindow()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7226/chat")
                .WithAutomaticReconnect()
                .Build();

            connection.On<string, string>("Receive", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var newMessage = $"{user}: {message}";
                    chatbox.Items.Insert(0, newMessage);
                });
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await connection.StartAsync();
                chatbox.Items.Add("Вы вошли в чат");
                sendBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отключении: {ex.Message}");
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await connection.InvokeAsync("Send", userTextBox.Text, messageTextBox.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}