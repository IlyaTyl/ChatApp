using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
    public partial class AuthRegWindow : Window
    {
        private HubConnection connection;

        public string AuthenticatedUsername { get; private set; }
        public AuthRegWindow()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7226/chat")
                .WithAutomaticReconnect()
                .Build();

            StartConnection();
        }

        private async void StartConnection()
        {
            try
            {
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к серверу: {ex.Message}");
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = usernameBox.Text.Trim();
                string password = passwordBox.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль");
                    return;
                }

                bool result = await connection.InvokeAsync<bool>("Login", username, password);

                if (result)
                {
                    AuthenticatedUsername = username;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}");
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = usernameBox.Text.Trim();
                string password = passwordBox.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль");
                    return;
                }

                bool result = await connection.InvokeAsync<bool>("Register", username, password);

                if (result)
                {
                    MessageBox.Show("Регистрация успешна. Теперь вы можете войти.");
                }
                else
                {
                    MessageBox.Show("Пользователь уже существует.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
            }
        }
    }
}
