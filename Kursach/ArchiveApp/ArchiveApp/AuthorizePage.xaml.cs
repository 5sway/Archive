using System;
using System.Collections.Generic;
using System.Data.Entity;
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

namespace ArchiveApp
{
    /// <summary>
    /// Логика взаимодействия для AuthorizePage.xaml
    /// </summary>
    public partial class AuthorizePage : Page
    {
        public AuthorizePage()
        {
            InitializeComponent();
        }

        private string GetUserRole(string login, string password)
        {
            var user = ArchiveBaseEntities.GetContext().User
                .Where(u => u.Login == login && u.Password == password)
                .Include(u => u.Role)
                .FirstOrDefault();
            return user?.Role?.Name;
        }

        public event Action OnUserAuthorized;
        private void AuthorizeUser()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            var user = ArchiveBaseEntities.GetContext().User.FirstOrDefault(u => u.Login == login);
            string role = GetUserRole(login, password);
            StringBuilder errorMessage = new StringBuilder();

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage.AppendLine("Введите пароль!");
            }

            if (string.IsNullOrWhiteSpace(login))
            {
                errorMessage.AppendLine("Введите логин!");
            }

            if (errorMessage.Length > 0)
            {
                MessageBox.Show(errorMessage.ToString());
                return;
            }

            if (user == null)
            {
                MessageBox.Show("Неверный логин!");
                return;
            }

            if (user.Password != password)
            {
                MessageBox.Show("Неверный пароль!");
                return;
            }
            OnUserAuthorized?.Invoke();
            Manager.MainFrame.Navigate(new MainMenuPage(role));
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            AuthorizeUser();
        }

        private void LoginBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordBox.Focus();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AuthorizeUser();
        }

        private void LoginBox_GotFocus(object sender, RoutedEventArgs e)
        {
            LoginText.Visibility = Visibility.Collapsed;
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text))
            {
                LoginText.Visibility = Visibility.Visible;
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordText.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                PasswordText.Visibility = Visibility.Visible;
            }
        }
    }
}
