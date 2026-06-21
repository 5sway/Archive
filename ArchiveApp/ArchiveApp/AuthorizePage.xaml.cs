using System;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ArchiveApp
{
    public partial class AuthorizePage : Page
    {
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        private string _pendingLogin;
        private string _pendingPassword;
        private string _captchaText;
        private int _failedAttempts = 0;
        private DateTime? _captchaGraceUntil = null;
        private readonly TimeSpan _captchaGracePeriod = TimeSpan.FromMinutes(1);
        private DispatcherTimer _errorTimer;
        private DispatcherTimer _smoothTimer;
        private DateTime _graceStartTime;

        public AuthorizePage()
        {
            InitializeComponent();
            SetupInitialState();
            ResetLoginUI();
            timeBeginPeriod(1);
            PreviewMouseDown += Page_PreviewMouseDown;
            PreviewTextInput += Page_PreviewTextInput;
        }

        ~AuthorizePage()
        {
            timeEndPeriod(1);
        }

        private void SetupInitialState()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed;
            _errorTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _errorTimer.Tick += (s, e) =>
            {
                HideError();
                _errorTimer.Stop();
            };

            _smoothTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _smoothTimer.Tick += SmoothTimer_Tick;
        }

        /// <summary>
        /// Плавно обновляет таймер обратного отсчёта в сообщении об ошибке.
        /// Обновляет оставшееся время каждые 100 мс для плавного отображения.
        /// </summary>
        private void SmoothTimer_Tick(object sender, EventArgs e)
        {
            if (!_captchaGraceUntil.HasValue) return;

            var remaining = _captchaGraceUntil.Value - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                _smoothTimer.Stop();
                _failedAttempts = 0;
                return;
            }

            if (ErrorMessage.Visibility == Visibility.Visible &&
                ErrorMessage.Text.Contains("Капча скрыта на"))
            {
                var baseMessage = ErrorMessage.Text.Split('(')[0].Trim();
                var secondsLeft = (int)remaining.TotalSeconds;
                ErrorMessage.Text = $"{baseMessage} (Капча скрыта на {secondsLeft} сек)";
            }
        }

        private void ShowError(string message)
        {
            if (_errorTimer.IsEnabled)
                _errorTimer.Stop();

            if (_captchaGraceUntil.HasValue &&
                DateTime.Now < _captchaGraceUntil.Value &&
                !message.Contains("Капча скрыта на"))
            {
                var remaining = _captchaGraceUntil.Value - DateTime.Now;
                var secondsLeft = (int)remaining.TotalSeconds;
                message += $" (Капча скрыта на {secondsLeft} сек)";
            }

            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;

            double baseFontSize = 14;
            Button activeButton;
            double buttonBottomPosition;

            if (LoginBtn.Visibility == Visibility.Visible)
            {
                activeButton = LoginBtn;
                buttonBottomPosition = activeButton.Margin.Top + activeButton.ActualHeight;
            }
            else
            {
                activeButton = CaptchaSubmitBtn;
                buttonBottomPosition = CaptchaContainer.Margin.Top + activeButton.Margin.Top + activeButton.ActualHeight;
            }

            int lineCount = message.Split('\n').Length;
            double newFontSize = baseFontSize;
            if (message.Length > 30 || lineCount > 1)
                newFontSize = Math.Max(10, baseFontSize - (message.Length / 20));
            ErrorMessage.FontSize = newFontSize;

            double textHeight = newFontSize * lineCount * 1.2;
            double newMarginTop = buttonBottomPosition + 20;
            if (textHeight > 20)
                newMarginTop += textHeight - 20;

            ErrorMessage.Margin = new Thickness(0, newMarginTop, 140, 0);
            ErrorMessage.HorizontalAlignment = HorizontalAlignment.Center;

            _errorTimer.Start();
        }

        private void HideError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.Text = "";
            ErrorMessage.FontSize = 14;
            ErrorMessage.Margin = new Thickness(0, 160, 140, 0);
        }

        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText();
            CaptchaImage.Source = CaptchaGenerator.GenerateCaptchaImage(_captchaText);
        }

        private void HideCaptchaUI()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed;
            CaptchaTextBox.Clear();
            CaptchaText.Visibility = Visibility.Visible;
            LoginBox.Visibility = Visibility.Visible;
            LoginText.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordText.Visibility = Visibility.Visible;
            LoginBtn.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;
            HideError();
            UpdatePlaceholderVisibility();
        }

        private bool IsCaptchaInGracePeriod()
        {
            bool isInGracePeriod = _captchaGraceUntil.HasValue && DateTime.Now < _captchaGraceUntil.Value;
            if (!isInGracePeriod && _captchaGraceUntil.HasValue && _failedAttempts >= 3)
            {
                _failedAttempts = 0;
                _captchaGraceUntil = null;
            }
            return isInGracePeriod;
        }

        private void RequestCaptcha()
        {
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
            CancelBtn.Visibility = Visibility.Collapsed;
            CaptchaContainer.Visibility = Visibility.Visible;
        }

        private void UpdatePlaceholderVisibility()
        {
            LoginText.Visibility = string.IsNullOrWhiteSpace(LoginBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PasswordText.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
            CaptchaText.Visibility = string.IsNullOrWhiteSpace(CaptchaTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Основной метод проверки учётных данных.
        /// Проверяет логин и пароль, отслеживает количество неудачных попыток.
        /// При трёх неудачных попытках запрашивает капчу и переводит интерфейс в соответствующий режим.
        /// </summary>
        private void VerifyCredentials()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            StringBuilder errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(login)) errorMessage.AppendLine("Введите логин!");
            if (string.IsNullOrWhiteSpace(password)) errorMessage.AppendLine("Введите пароль!");

            if (errorMessage.Length > 0)
            {
                ShowError(errorMessage.ToString());
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable()
                .FirstOrDefault(u => u.Login == login);

            if (user == null || user.Password != password)
            {
                if (!IsCaptchaInGracePeriod()) _failedAttempts++;
                ShowError(user == null ? "Неверный логин!" : "Неверный пароль!");
                _pendingLogin = null;
                _pendingPassword = null;

                if (IsCaptchaInGracePeriod()) return;

                if (_failedAttempts >= 3)
                {
                    HideError();
                    _pendingLogin = login;
                    _pendingPassword = password;
                    RequestCaptcha();
                }
                return;
            }

            _pendingLogin = login;
            _pendingPassword = password;
            AuthorizeUser();
        }

        /// <summary>
        /// Проверяет введённую капчу и при успехе завершает авторизацию.
        /// Устанавливает тайм-аут для повторного запроса капчи после успешного ввода.
        /// </summary>
        private void AuthorizeWithCaptcha()
        {
            string enteredCaptcha = CaptchaTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText)
            {
                ShowError("Неверная капча! Попробуйте еще раз.");
                GenerateNewCaptcha();
                return;
            }

            _graceStartTime = DateTime.Now;
            _captchaGraceUntil = _graceStartTime.Add(_captchaGracePeriod);
            _smoothTimer.Start();

            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword))
            {
                ShowError("Введите логин и пароль заново!");
                _failedAttempts = 0;
                HideCaptchaUI();
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable()
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)
            {
                _failedAttempts++;
                ShowError("Неверный логин или пароль!");
                HideCaptchaUI();
                return;
            }

            HideCaptchaUI();
            _failedAttempts = 0;
            HideError();
            AuthorizeUser();
        }

        private void ResetLoginUI(bool clearInputs = true)
        {
            HideCaptchaUI();
            HideError();
            if (_errorTimer.IsEnabled)
                _errorTimer.Stop();

            if (clearInputs)
            {
                LoginBox.Clear();
                PasswordBox.Clear();
            }

            UpdatePlaceholderVisibility();
        }

        private string GetUserRole(string login, string password)
        {
            var user = ArchiveBaseEntities.GetContext().User
                .Where(u => u.Login == login && u.Password == password)
                .Include(u => u.Role)
                .FirstOrDefault();
            return user?.Role?.Name;
        }

        /// <summary>
        /// Завершает процесс авторизации: устанавливает данные текущего пользователя,
        /// генерирует событие, логирует успешный вход и переходит на главное меню.
        /// </summary>
        private void AuthorizeUser()
        {
            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword))
            {
                ShowError("Ошибка авторизации: данные отсутствуют!");
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)
            {
                ShowError("Ошибка авторизации: неверные данные!");
                _pendingLogin = null;
                _pendingPassword = null;
                return;
            }

            string role = GetUserRole(_pendingLogin, _pendingPassword);
            UserData.CurrentUserId = user.Id;
            UserData.CurrentUserRole = role;
            UserData.CurrentUserName = user.Name;

            OnUserAuthorized?.Invoke();
            Manager.MainFrame.Navigate(new MainMenuPage(role));
            AuditService.Log("Успешная авторизация", "User",
            $"Пользователь {user.Name} {user.Last_Name} вошёл в систему");
            ResetLoginUI();
        }

        public void ResetAuthorizationState()
        {
            _pendingLogin = null;
            _pendingPassword = null;
            _failedAttempts = 0;
            ResetLoginUI(true);
            UpdatePlaceholderVisibility();
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

        private void CaptchaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AuthorizeWithCaptcha();
        }

        private void LoginBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void CaptchaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void CaptchaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        private void LoginText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginBox.Focus();
        }

        private void PasswordText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PasswordBox.Focus();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginBox.Clear();
            PasswordBox.Clear();
            UpdatePlaceholderVisibility();
        }

        private void CaptchaText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CaptchaTextBox.Focus();
        }

        private void Page_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;

            bool isEmptySpace = false;
            while (clickedElement != null)
            {
                if (clickedElement is Grid grid && grid.Name == "MainGrid")
                {
                    isEmptySpace = true;
                    break;
                }
                if (clickedElement is Button || clickedElement is TextBlock ||
                    clickedElement is Image || clickedElement is TextBox ||
                    clickedElement is PasswordBox)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }
            if (isEmptySpace)
            {
                var focusedElement = Keyboard.FocusedElement;
                if (focusedElement == LoginBox || focusedElement == PasswordBox || focusedElement == CaptchaTextBox)
                {
                    Keyboard.ClearFocus();
                    UpdatePlaceholderVisibility();
                }
            }
        }

        private void Page_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(LoginBox.IsFocused || PasswordBox.IsFocused || CaptchaTextBox.IsFocused))
            {
                LoginBox.Focus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (Keyboard.FocusedElement == LoginBox || Keyboard.FocusedElement == PasswordBox || Keyboard.FocusedElement == CaptchaTextBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }

        public event Action OnUserAuthorized;
    }
}