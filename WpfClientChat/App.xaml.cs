using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfClientChat
{
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var authWindow = new AuthRegWindow();
                bool? result = authWindow.ShowDialog();

                if (result == true)
                {
                    var mainWindow = new MainWindow(authWindow.AuthenticatedUsername);
                    mainWindow.Show(); // Сначала показываем окно

                    Application.Current.MainWindow = mainWindow;
                    bool started = await mainWindow.StartConnectionAsync();

                    if (!started)
                    {
                        MessageBox.Show("Не удалось подключиться к серверу. Приложение будет закрыто.");
                        mainWindow.Close();
                        Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}");
                Shutdown();
            }
        }
    }

}
