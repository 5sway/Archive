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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            var user = ArchiveBaseEntities.GetContext().User.FirstOrDefault(u => u.Login == login);

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин!");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль!");
                return;
            }

            if (user == null || user.Password != password || user.Login != login)
            {
                MessageBox.Show("Неверный логин или пароль!");
                return;
            }

            if (user.Password != password)
            {
                MessageBox.Show("Неверный пароль!");
                return;
            }

            Manager.MainFrame.Navigate(new MainMenuPage());
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
