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
        private string _captchaText;
        private bool _captchaPassed = false;
        private bool _credentialsVerified = false;
        public AuthorizePage()
        {
            InitializeComponent();
            SetupInitialState();
        }
        private void SetupInitialState()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed;
        }

        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText();
            CaptchaImage.Source = CaptchaGenerator.GenerateCaptchaImage(_captchaText);
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

        private void VerifyCredentials()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            var user = ArchiveBaseEntities.GetContext().User.FirstOrDefault(u => u.Login == login);
            StringBuilder errorMessage = new StringBuilder();

            if (string.IsNullOrWhiteSpace(password))
                errorMessage.AppendLine("Введите пароль!");

            if (string.IsNullOrWhiteSpace(login))
                errorMessage.AppendLine("Введите логин!");

            if (errorMessage.Length > 0)
            {
                MessageBox.Show(errorMessage.ToString());
                return;
            }

            if (user == null)
            {
                MessageBox.Show("Неверный логин!");
                RequestCaptcha();
                return;
            }

            if (user.Password != password)
            {
                MessageBox.Show("Неверный пароль!");
                RequestCaptcha();
                return;
            }

            _credentialsVerified = true;

            if (_captchaPassed)
            {
                AuthorizeUser();
            }
            else
            {
                GenerateNewCaptcha();
                ShowCaptchaStep();
            }
        }

        private void RequestCaptcha()
        {
            _captchaPassed = false;
            GenerateNewCaptcha();
            ShowCaptchaStep();
        }

        private void ShowCaptchaStep()
        {
            LoginBox.Visibility = Visibility.Collapsed;
            LoginText.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordText.Visibility = Visibility.Collapsed;
            LoginBtn.Visibility = Visibility.Collapsed;
            CaptchaContainer.Visibility = Visibility.Visible;
        }

        private void AuthorizeWithCaptcha()
        {
            string enteredCaptcha = CaptchaTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText)
            {
                MessageBox.Show("Неверная капча! Попробуйте еще раз.");
                GenerateNewCaptcha();
                return;
            }

            if (_credentialsVerified)
            {
                AuthorizeUser();
            }
            else
            {
                MessageBox.Show("Введенн неверный логин или пароль!");
            }

            _captchaPassed = true;
            LoginBox.Clear();
            PasswordBox.Clear();
            LoginBox.Visibility = Visibility.Visible;
            LoginText.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordText.Visibility = Visibility.Visible;
            LoginBtn.Visibility = Visibility.Visible;
            CaptchaContainer.Visibility = Visibility.Collapsed;
            CaptchaTextBox.Clear();
        }
        private void AuthorizeUser()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string role = GetUserRole(login, password);

            UserData.CurrentUserRole = role;
            OnUserAuthorized?.Invoke();
            Manager.MainFrame.Navigate(new MainMenuPage(role));
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            VerifyCredentials();
        }

        private void CaptchaSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
                AuthorizeWithCaptcha();
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateNewCaptcha();
        }

        private void LoginBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordBox.Focus();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                VerifyCredentials();
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

        private void CaptchaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed;
        }

        private void CaptchaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CaptchaTextBox.Text))
            {
                CaptchaText.Visibility = Visibility.Visible;
            }
        }

        private void CaptchaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AuthorizeWithCaptcha();
        }
    }
}
