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
using System.Windows.Threading;

namespace ArchiveApp
{
    /// <summary>
    /// Логика взаимодействия для AuthorizePage.xaml
    /// </summary>
    public partial class AuthorizePage : Page
    {
        private string _pendingLogin;
        private string _pendingPassword;
        private string _captchaText;
        private bool _credentialsVerified = false;
        private int _failedAttempts = 0;
        private static Random _random = new Random();
        private DateTime? _captchaPassedTime = null;
        private readonly TimeSpan _captchaValidPeriod = TimeSpan.FromMinutes(10);
        private DateTime? _captchaGraceUntil = null;
        private readonly TimeSpan _captchaGracePeriod = TimeSpan.FromMinutes(1);


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
            // Получение введенных данных
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            // Поиск пользователя в базе данных (без учета регистра)
            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable().FirstOrDefault(u => u.Login == login);
            StringBuilder errorMessage = new StringBuilder();

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(password))
                errorMessage.AppendLine("Введите пароль!");

            if (string.IsNullOrWhiteSpace(login))
                errorMessage.AppendLine("Введите логин!");

            // Вывод ошибок, если есть
            if (errorMessage.Length > 0)
            {
                MessageBox.Show(errorMessage.ToString());
                return;
            }

            // Проверка соответствия логина и пароля
            if (user == null || user.Password != password)
            {
                _failedAttempts++; // Увеличение счетчика неудачных попыток

                MessageBox.Show(user == null ? "Неверный логин!" : "Неверный пароль!", "", MessageBoxButton.OK, MessageBoxImage.Error);

                // Если было 2 или более неудачных попыток и не в "периоде милосердия"
                if (_failedAttempts >= 3 && !IsCaptchaInGracePeriod())
                {
                    RequestCaptcha(); // Требование ввода капчи
                }

                return;
            }

            // Сохранение введенных данных
            _pendingLogin = login;
            _pendingPassword = password;

            // Дополнительная проверка на необходимость капчи
            if (_failedAttempts >= 3 && !IsCaptchaInGracePeriod())
            {
                RequestCaptcha();
                return;
            }

            // Авторизация пользователя
            AuthorizeUser();
        }

        private void HideCaptchaUI()
        {
            // Скрытие контейнера капчи
            CaptchaContainer.Visibility = Visibility.Collapsed;

            // Очистка поля ввода капчи
            CaptchaTextBox.Clear();
            CaptchaText.Visibility = Visibility.Visible;

            // Восстановление видимости стандартных элементов входа
            LoginBox.Visibility = Visibility.Visible;
            LoginText.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordText.Visibility = Visibility.Visible;
            LoginBtn.Visibility = Visibility.Visible;

            // Обновление видимости подсказок
            UpdatePlaceholderVisibility();
        }

        private bool IsCaptchaStillValid()
        {
            return _captchaPassedTime.HasValue && (DateTime.Now - _captchaPassedTime.Value) <= _captchaValidPeriod;
        }

        private bool IsCaptchaInGracePeriod()
        {
            return _captchaGraceUntil.HasValue && DateTime.Now < _captchaGraceUntil.Value;
        }

        private void RequestCaptcha()
        {
            // Генерация новой капчи
            GenerateNewCaptcha();

            // Отображение интерфейса капчи
            ShowCaptchaStep();
        }


        private void ShowCaptchaStep()
        {
            // Скрытие стандартных элементов входа
            LoginBox.Visibility = Visibility.Collapsed;
            LoginText.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordText.Visibility = Visibility.Collapsed;
            LoginBtn.Visibility = Visibility.Collapsed;

            // Отображение контейнера капчи
            CaptchaContainer.Visibility = Visibility.Visible;
        }

        private void UpdatePlaceholderVisibility()
        {
            LoginText.Visibility = string.IsNullOrWhiteSpace(LoginBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PasswordText.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AuthorizeWithCaptcha()
        {
            // Получение введенной капчи
            string enteredCaptcha = CaptchaTextBox.Text.Trim();

            // Проверка капчи
            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText)
            {
                MessageBox.Show("Неверная капча! Попробуйте еще раз.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                GenerateNewCaptcha(); // Генерация новой капчи при ошибке
                return;
            }

            // Запись времени успешного прохождения капчи
            _captchaPassedTime = DateTime.Now;
            _captchaGraceUntil = DateTime.Now.Add(_captchaGracePeriod); // Установка "периода милосердия"
            HideCaptchaUI(); // Скрытие интерфейса капчи

            // Если учетные данные уже проверены, авторизовать пользователя
            if (_credentialsVerified)
            {
                AuthorizeUser();
            }
        }

        private void ResetLoginUI(bool clearInputs = true)
        {
            // Скрытие интерфейса капчи
            HideCaptchaUI();

            // Очистка полей ввода (если требуется)
            if (clearInputs)
            {
                LoginBox.Clear();
                PasswordBox.Clear();
            }
        }

        private void AuthorizeUser()
        {
            // Использование сохраненных учетных данных
            string login = _pendingLogin;
            string password = _pendingPassword;

            // Получение роли пользователя
            string role = GetUserRole(login, password);

            // Поиск пользователя в базе данных
            var user = ArchiveBaseEntities.GetContext().User
                .FirstOrDefault(u => u.Login == login);

            // Сохранение ID текущего пользователя
            if (user != null)
            {
                UserData.CurrentUserId = user.Id;
            }

            // Сброс счетчика неудачных попыток
            _failedAttempts = 0;

            // Сохранение роли текущего пользователя
            UserData.CurrentUserRole = role;

            // Вызов события успешной авторизации
            OnUserAuthorized?.Invoke();

            // Переход на главную страницу с передачей роли
            Manager.MainFrame.Navigate(new MainMenuPage(role));

            // Сброс интерфейса входа
            ResetLoginUI();
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