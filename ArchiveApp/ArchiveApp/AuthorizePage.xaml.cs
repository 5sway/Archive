using System;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ArchiveApp
{
    public partial class AuthorizePage : Page
    {
        // Импорт функций для точного таймера
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        private string _pendingLogin;              // Логин, ожидающий проверки
        private string _pendingPassword;           // Пароль, ожидающий проверки
        private string _captchaText;               // Текст текущей капчи
        private int _failedAttempts = 0;           // Счетчик неудачных попыток входа
        private DateTime? _captchaGraceUntil = null; // Время окончания режима милосердия капчи
        private readonly TimeSpan _captchaGracePeriod = TimeSpan.FromMinutes(1); // Длительность режима милосердия
        private DispatcherTimer _errorTimer;       // Таймер для скрытия сообщений об ошибках
        private DispatcherTimer _smoothTimer;      // Таймер для плавного обновления счетчика
        private DateTime _graceStartTime;          // Время начала периода милосердия

        public AuthorizePage()
        {
            InitializeComponent();                 // Инициализация компонентов страницы
            SetupInitialState();                  // Настройка начального состояния интерфейса
            ResetLoginUI();                       // Сброс UI до начального состояния
            timeBeginPeriod(1);                   // Установка высокой точности таймера (1 мс)
        }

        ~AuthorizePage()
        {
            timeEndPeriod(1);                     // Восстановление стандартной точности таймера
        }

        private void SetupInitialState()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed; // Скрытие контейнера капчи

            // Настройка таймера для сообщений об ошибках
            _errorTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromSeconds(3) // Установка интервала в 3 секунды
            };
            _errorTimer.Tick += (s, e) =>         // Обработчик события таймера
            {
                HideError();                      // Скрытие ошибки
                _errorTimer.Stop();               // Остановка таймера
            };

            // Настройка плавного таймера для обновления счетчика
            _smoothTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(100) // Обновление каждые 100 мс
            };
            _smoothTimer.Tick += SmoothTimer_Tick; // Подключение обработчика
        }

        private void SmoothTimer_Tick(object sender, EventArgs e)
        {
            if (!_captchaGraceUntil.HasValue) return;

            var remaining = _captchaGraceUntil.Value - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                _smoothTimer.Stop();              // Остановка таймера по истечении времени
                _failedAttempts = 0;              // Сброс счетчика ошибок
                return;
            }

            // Обновление текста ошибки с плавным уменьшением счетчика
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
            if (_errorTimer.IsEnabled)            // Остановка активного таймера перед новым показом
                _errorTimer.Stop();

            // Добавление информации о таймере, если активен режим милосердия
            if (_captchaGraceUntil.HasValue &&
                DateTime.Now < _captchaGraceUntil.Value &&
                !message.Contains("Капча скрыта на"))
            {
                var remaining = _captchaGraceUntil.Value - DateTime.Now;
                var secondsLeft = (int)remaining.TotalSeconds;
                message += $" (Капча скрыта на {secondsLeft} сек)";
            }

            ErrorMessage.Text = message;          // Установка текста ошибки
            ErrorMessage.Visibility = Visibility.Visible; // Показ сообщения об ошибке

            double baseFontSize = 14;             // Базовый размер шрифта
            Button activeButton;                  // Активная кнопка (LoginBtn или CaptchaSubmitBtn)
            double buttonBottomPosition;          // Позиция нижней границы активной кнопки

            // Определяем активную кнопку и её положение
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

            int lineCount = message.Split('\n').Length; // Подсчет строк в сообщении
            double newFontSize = baseFontSize;  // Новый размер шрифта
            if (message.Length > 30 || lineCount > 1) // Уменьшение шрифта для длинных сообщений
                newFontSize = Math.Max(10, baseFontSize - (message.Length / 20));
            ErrorMessage.FontSize = newFontSize;  // Применение нового размера шрифта

            double textHeight = newFontSize * lineCount * 1.2; // Высота текста с учетом строк
            double newMarginTop = buttonBottomPosition + 20; // Отступ под кнопкой
            if (textHeight > 20)                 // Корректировка отступа для длинного текста
                newMarginTop += textHeight - 20;

            ErrorMessage.Margin = new Thickness(0, newMarginTop, 140, 0); // Установка отступов
            ErrorMessage.HorizontalAlignment = HorizontalAlignment.Center; // Центрирование текста

            _errorTimer.Start();                  // Запуск таймера для скрытия ошибки
        }

        private void HideError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed; // Скрытие сообщения об ошибке
            ErrorMessage.Text = "";               // Очистка текста ошибки
            ErrorMessage.FontSize = 14;           // Сброс размера шрифта
            ErrorMessage.Margin = new Thickness(0, 160, 140, 0); // Сброс отступов
        }

        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(); // Генерация текста капчи
            CaptchaImage.Source = CaptchaGenerator.GenerateCaptchaImage(_captchaText); // Создание изображения
        }

        private void HideCaptchaUI()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed; // Скрытие капчи
            CaptchaTextBox.Clear();               // Очистка поля ввода капчи
            CaptchaText.Visibility = Visibility.Visible; // Показ подсказки капчи
            LoginBox.Visibility = Visibility.Visible; // Показ поля логина
            LoginText.Visibility = Visibility.Visible; // Показ подсказки логина
            PasswordBox.Visibility = Visibility.Visible; // Показ поля пароля
            PasswordText.Visibility = Visibility.Visible; // Показ подсказки пароля
            LoginBtn.Visibility = Visibility.Visible; // Показ кнопки входа
            CancelBtn.Visibility = Visibility.Visible; // Показ кнопки отмена
            HideError();                          // Скрытие сообщения об ошибке
            UpdatePlaceholderVisibility();        // Обновление видимости подсказок
        }

        private bool IsCaptchaInGracePeriod()
        {
            bool isInGracePeriod = _captchaGraceUntil.HasValue && DateTime.Now < _captchaGraceUntil.Value;
            if (!isInGracePeriod && _captchaGraceUntil.HasValue && _failedAttempts >= 3)
            {
                _failedAttempts = 0;              // Сброс счетчика
                _captchaGraceUntil = null;        // Очищаем время окончания режима
            }
            return isInGracePeriod;
        }

        private void RequestCaptcha()
        {
            GenerateNewCaptcha();                 // Создание новой капчи
            ShowCaptchaStep();                    // Переход к интерфейсу капчи
        }

        private void ShowCaptchaStep()
        {
            LoginBox.Visibility = Visibility.Collapsed; // Скрытие поля логина
            LoginText.Visibility = Visibility.Collapsed; // Скрытие подсказки логина
            PasswordBox.Visibility = Visibility.Collapsed; // Скрытие поля пароля
            PasswordText.Visibility = Visibility.Collapsed; // Скрытие подсказки пароля
            LoginBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки входа
            CancelBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки отмена
            CaptchaContainer.Visibility = Visibility.Visible; // Показ контейнера капчи
        }

        private void UpdatePlaceholderVisibility()
        {
            LoginText.Visibility = string.IsNullOrWhiteSpace(LoginBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            PasswordText.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void VerifyCredentials()
        {
            string login = LoginBox.Text.Trim();  // Получение логина без пробелов
            string password = PasswordBox.Password.Trim(); // Получение пароля без пробелов

            StringBuilder errorMessage = new StringBuilder(); // Сбор ошибок ввода
            if (string.IsNullOrWhiteSpace(login)) errorMessage.AppendLine("Введите логин!");
            if (string.IsNullOrWhiteSpace(password)) errorMessage.AppendLine("Введите пароль!");

            if (errorMessage.Length > 0)          // Показ ошибки, если поля пустые
            {
                ShowError(errorMessage.ToString());
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable() // Поиск пользователя
                .FirstOrDefault(u => u.Login == login);

            if (user == null || user.Password != password) // Проверка корректности данных
            {
                if (!IsCaptchaInGracePeriod()) _failedAttempts++; // Увеличение счетчика
                ShowError(user == null ? "Неверный логин!" : "Неверный пароль!");
                _pendingLogin = null;
                _pendingPassword = null;

                if (IsCaptchaInGracePeriod()) return; // Пропуск капчи в режиме милосердия

                if (_failedAttempts >= 3)        // Показ капчи после 3 неудач
                {
                    HideError();
                    _pendingLogin = login;
                    _pendingPassword = password;
                    RequestCaptcha();
                }
                return;
            }

            _pendingLogin = login;                // Сохранение верного логина
            _pendingPassword = password;          // Сохранение верного пароля
            AuthorizeUser();                      // Авторизация пользователя
        }

        private void AuthorizeWithCaptcha()
        {
            string enteredCaptcha = CaptchaTextBox.Text.Trim(); // Получение введенной капчи

            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText)
            {
                ShowError("Неверная капча! Попробуйте еще раз."); // Сообщение об ошибке
                GenerateNewCaptcha();           // Обновление капчи
                return;
            }

            _graceStartTime = DateTime.Now;
            _captchaGraceUntil = _graceStartTime.Add(_captchaGracePeriod);
            _smoothTimer.Start();                 // Запуск плавного таймера

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
            HideCaptchaUI();                    // Скрытие интерфейса капчи
            HideError();                        // Скрытие сообщения об ошибке
            if (_errorTimer.IsEnabled)          // Остановка таймера ошибки
                _errorTimer.Stop();

            if (clearInputs)                    // Очистка полей ввода, если требуется
            {
                LoginBox.Clear();               // Очистка логина
                PasswordBox.Clear();            // Очистка пароля
            }

            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
        }

        private string GetUserRole(string login, string password)
        {
            var user = ArchiveBaseEntities.GetContext().User // Поиск пользователя в базе
                .Where(u => u.Login == login && u.Password == password)
                .Include(u => u.Role)           // Подключение данных о роли
                .FirstOrDefault();
            return user?.Role?.Name;            // Возврат имени роли или null
        }

        private void AuthorizeUser()
        {
            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword))
            {
                ShowError("Ошибка авторизации: данные отсутствуют!"); // Сообщение об ошибке
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User // Поиск пользователя для авторизации
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)                   // Обработка ошибки авторизации
            {
                ShowError("Ошибка авторизации: неверные данные!"); // Сообщение об ошибке
                _pendingLogin = null;           // Очистка логина
                _pendingPassword = null;        // Очистка пароля
                return;
            }

            string role = GetUserRole(_pendingLogin, _pendingPassword); // Получение роли пользователя
            UserData.CurrentUserId = user.Id;   // Сохранение ID пользователя
            UserData.CurrentUserRole = role;    // Сохранение роли в глобальных данных
            UserData.CurrentUserName = user.Name; // Сохранение имени в глобальных данных

            OnUserAuthorized?.Invoke();         // Вызов события авторизации
            Manager.MainFrame.Navigate(new MainMenuPage(role)); // Переход на главную страницу
            ResetLoginUI();                     // Сброс интерфейса
        }

        public void ResetAuthorizationState()
        {
            _pendingLogin = null;               // Очистка ожидающего логина
            _pendingPassword = null;            // Очистка ожидающего пароля
            _failedAttempts = 0;                // Сброс счетчика неудач
            ResetLoginUI(true);                 // Полный сброс интерфейса
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            VerifyCredentials();                // Проверка учетных данных при нажатии кнопки
        }

        private void CaptchaSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            AuthorizeWithCaptcha();             // Авторизация с капчей при нажатии кнопки
        }

        private void RefreshCaptcha_Click(object sender, RoutedEventArgs e)
        {
            GenerateNewCaptcha();               // Обновление капчи при нажатии кнопки
        }

        private void LoginBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)             // Переход к полю пароля по Enter
                PasswordBox.Focus();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)             // Проверка данных по Enter в поле пароля
                VerifyCredentials();
        }

        private void CaptchaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)             // Авторизация с капчей по Enter
                AuthorizeWithCaptcha();
        }

        private void LoginBox_GotFocus(object sender, RoutedEventArgs e)
        {
            LoginText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на логине
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на пароле
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
        }

        private void CaptchaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на капче
        }

        private void CaptchaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CaptchaText.Visibility = string.IsNullOrWhiteSpace(CaptchaTextBox.Text) ?
                Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginText.Visibility = Visibility.Collapsed; // Скрытие подсказки
            LoginBox.Focus();                           // Перевод фокуса на поле логина
        }

        private void PasswordText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PasswordText.Visibility = Visibility.Collapsed; // Скрытие подсказки
            PasswordBox.Focus();                           // Перевод фокуса на поле пароля
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginBox.Clear();                     // Очистка поля логина
            PasswordBox.Clear();                  // Очистка поля пароля
            UpdatePlaceholderVisibility();        // Обновление видимости подсказок
        }

        private void CaptchaText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed; // Скрытие подсказки
            CaptchaTextBox.Focus();                           // Перевод фокуса на поле капчи
        }
        
        public event Action OnUserAuthorized;     // Событие успешной авторизации
    }
}