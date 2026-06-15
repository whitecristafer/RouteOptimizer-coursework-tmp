using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace RouteOptimizer;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        try
        {
            var login = LoginBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ErrorText.Text = "Введите логин и пароль.";
                return;
            }

            using var db = new AppDbContext();
            var user = await db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Username == login && x.PasswordHash == password && x.IsActive);

            if (user?.Role is null)
            {
                ErrorText.Text = "Неверный логин или пароль.";
                return;
            }

            Session.CurrentUser = user;
            Session.CurrentRoleName = user.Role.RoleName;

            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка входа: {ex.Message}", "RouteOptimizer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
