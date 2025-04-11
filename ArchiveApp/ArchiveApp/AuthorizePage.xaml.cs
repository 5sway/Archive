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
    public partial class AuthorizePage : Page
    {
        private string _pendingLogin;              // Логин, ожидающий проверки
        private string _pendingPassword;           // Пароль, ожидающий проверки
        private string _captchaText;               // Текст текущей капчи
        private bool _credentialsVerified = false; // Флаг успешной проверки учетных данных
        private int _failedAttempts = 0;           // Счетчик неудачных попыток входа
        private DateTime? _captchaGraceUntil = null; // Время окончания режима милосердия капчи
        private readonly TimeSpan _captchaGracePeriod = TimeSpan.FromMinutes(1); // Длительность режима милосердия
        private DispatcherTimer _errorTimer;       // Таймер для скрытия сообщений об ошибок

        public AuthorizePage()
        {
            InitializeComponent();                 // Инициализация компонентов страницы
            SetupInitialState();                  // Настройка начального состояния интерфейса
            ResetLoginUI();                       // Сброс UI до начального состояния
        }

        private void SetupInitialState()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed; // Скрытие контейнера капчи
            _errorTimer = new DispatcherTimer   // Создание таймера для сообщений об ошибках
            {
                Interval = TimeSpan.FromSeconds(3) // Установка интервала в 3 секунды
            };
            _errorTimer.Tick += (s, e) =>       // Обработчик события таймера
            {
                HideError();                    // Скрытие ошибки
                _errorTimer.Stop();             // Остановка таймера
            };
        }

        private void ShowError(string message)
        {
            if (_errorTimer.IsEnabled)          // Остановка активного таймера перед новым показом
                _errorTimer.Stop();

            ErrorMessage.Text = message;        // Установка текста ошибки
            ErrorMessage.Visibility = Visibility.Visible; // Показ сообщения об ошибке

            double baseFontSize = 14;           // Базовый размер шрифта
            Button activeButton;                // Активная кнопка (LoginBtn или CaptchaSubmitBtn)
            double buttonBottomPosition;        // Позиция нижней границы активной кнопки

            // Определяем активную кнопку и её положение
            if (LoginBtn.Visibility == Visibility.Visible)
            {
                activeButton = LoginBtn;
                buttonBottomPosition = activeButton.Margin.Top + activeButton.ActualHeight;
            }
            else if (CaptchaSubmitBtn.Visibility == Visibility.Visible)
            {
                activeButton = CaptchaSubmitBtn;
                // Учитываем смещение CaptchaContainer и его дочерних элементов
                buttonBottomPosition = CaptchaContainer.Margin.Top + activeButton.Margin.Top + activeButton.ActualHeight;
            }
            else
            {
                activeButton = LoginBtn;        // По умолчанию используем LoginBtn
                buttonBottomPosition = activeButton.Margin.Top + activeButton.ActualHeight;
            }

            int lineCount = message.Split('\n').Length; // Подсчет строк в сообщении
            double newFontSize = baseFontSize;  // Новый размер шрифта
            if (message.Length > 30 || lineCount > 1) // Уменьшение шрифта для длинных сообщений
                newFontSize = Math.Max(10, baseFontSize - (message.Length / 20));
            ErrorMessage.FontSize = newFontSize;// Применение нового размера шрифта

            double textHeight = newFontSize * lineCount * 1.2; // Высота текста с учетом строк
            double newMarginTop = buttonBottomPosition + 20; // Отступ под кнопкой + увеличенный зазор (было 10, стало 20)
            if (textHeight > 20)                // Корректировка отступа для длинного текста
                newMarginTop += textHeight - 20;

            ErrorMessage.Margin = new Thickness(0, newMarginTop, 140, 0); // Установка отступов
            ErrorMessage.HorizontalAlignment = HorizontalAlignment.Center; // Центрирование текста

            _errorTimer.Start();                // Запуск таймера для скрытия ошибки
        }

        private void HideError()
        {
            ErrorMessage.Visibility = Visibility.Collapsed; // Скрытие сообщения об ошибке
            ErrorMessage.Text = "";             // Очистка текста ошибки
            ErrorMessage.FontSize = 14;         // Сброс размера шрифта
            ErrorMessage.Margin = new Thickness(0, 160, 140, 0); // Сброс отступов на начальные значения
        }

        private void GenerateNewCaptcha()
        {
            _captchaText = CaptchaGenerator.GenerateCaptchaText(); // Генерация текста капчи
            CaptchaImage.Source = CaptchaGenerator.GenerateCaptchaImage(_captchaText); // Создание изображения капчи
        }

        private string GetUserRole(string login, string password)
        {
            var user = ArchiveBaseEntities.GetContext().User // Поиск пользователя в базе
                .Where(u => u.Login == login && u.Password == password)
                .Include(u => u.Role)           // Подключение данных о роли
                .FirstOrDefault();
            return user?.Role?.Name;            // Возврат имени роли или null
        }

        public event Action OnUserAuthorized;   // Событие успешной авторизации

        private void VerifyCredentials()
        {
            string login = LoginBox.Text.Trim(); // Получение логина без пробелов
            string password = PasswordBox.Password.Trim(); // Получение пароля без пробелов

            StringBuilder errorMessage = new StringBuilder(); // Сбор ошибок ввода
            if (string.IsNullOrWhiteSpace(password)) // Проверка пустого пароля
                errorMessage.AppendLine("Введите пароль!");
            if (string.IsNullOrWhiteSpace(login))   // Проверка пустого логина
                errorMessage.AppendLine("Введите логин!");

            if (errorMessage.Length > 0)            // Показ ошибки, если поля пустые
            {
                ShowError(errorMessage.ToString());
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable() // Поиск пользователя
                .FirstOrDefault(u => u.Login == login);

            if (user == null || user.Password != password) // Проверка корректности данных
            {
                _failedAttempts++;                  // Увеличение счетчика неудачных попыток
                ShowError(user == null ? "Неверный логин!" : "Неверный пароль!"); // Сообщение об ошибке
                _pendingLogin = null;               // Очистка неверного логина
                _pendingPassword = null;            // Очистка неверного пароля
                _credentialsVerified = false;       // Сброс флага проверки

                if (IsCaptchaInGracePeriod())       // Пропуск капчи в режиме милосердия
                    return;

                if (_failedAttempts >= 3)           // Показ капчи после 3 неудач
                {
                    HideError();                    // Скрытие ошибки
                    _pendingLogin = login;          // Сохранение логина для проверки
                    _pendingPassword = password;    // Сохранение пароля для проверки
                    RequestCaptcha();               // Запрос капчи
                }
                return;
            }

            _pendingLogin = login;              // Сохранение верного логина
            _pendingPassword = password;        // Сохранение верного пароля
            _credentialsVerified = true;        // Установка флага успешной проверки
            AuthorizeUser();                    // Авторизация пользователя
        }

        private void HideCaptchaUI()
        {
            CaptchaContainer.Visibility = Visibility.Collapsed; // Скрытие капчи
            CaptchaTextBox.Clear();             // Очистка поля ввода капчи
            CaptchaText.Visibility = Visibility.Visible; // Показ подсказки капчи
            LoginBox.Visibility = Visibility.Visible; // Показ поля логина
            LoginText.Visibility = Visibility.Visible; // Показ подсказки логина
            PasswordBox.Visibility = Visibility.Visible; // Показ поля пароля
            PasswordText.Visibility = Visibility.Visible; // Показ подсказки пароля
            LoginBtn.Visibility = Visibility.Visible; // Показ кнопки входа
            CancelBtn.Visibility = Visibility.Visible; // Показ кнопки отмена
            HideError();                        // Скрытие сообщения об ошибке
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
        }

        private bool IsCaptchaInGracePeriod()
        {
            return _captchaGraceUntil.HasValue && DateTime.Now < _captchaGraceUntil.Value; // Проверка активности режима милосердия
        }

        private void RequestCaptcha()
        {
            GenerateNewCaptcha();               // Создание новой капчи
            ShowCaptchaStep();                  // Переход к интерфейсу капчи
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
            LoginText.Visibility = string.IsNullOrWhiteSpace(LoginBox.Text) ? Visibility.Visible : Visibility.Collapsed; // Управление подсказкой логина
            PasswordText.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed; // Управление подсказкой пароля
        }

        private void AuthorizeWithCaptcha()
        {
            string enteredCaptcha = CaptchaTextBox.Text.Trim(); // Получение введенной капчи

            if (string.IsNullOrWhiteSpace(enteredCaptcha) || enteredCaptcha != _captchaText) // Проверка корректности капчи
            {
                ShowError("Неверная капча! Попробуйте еще раз."); // Сообщение об ошибке
                GenerateNewCaptcha();           // Обновление капчи
                return;
            }

            _captchaGraceUntil = DateTime.Now.Add(_captchaGracePeriod); // Установка режима милосердия

            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword)) // Проверка наличия данных
            {
                ShowError("Введите логин и пароль заново!"); // Сообщение об ошибке
                HideCaptchaUI();                // Возврат к форме входа
                return;
            }

            var user = ArchiveBaseEntities.GetContext().User.AsEnumerable() // Повторная проверка данных
                .FirstOrDefault(u => u.Login == _pendingLogin && u.Password == _pendingPassword);

            if (user == null)                   // Обработка неверных данных
            {
                _failedAttempts++;              // Увеличение счетчика неудач
                ShowError("Неверный логин или пароль!"); // Сообщение об ошибке
                HideCaptchaUI();                // Возврат к форме входа
                return;
            }

            _credentialsVerified = true;        // Установка флага проверки
            HideCaptchaUI();                    // Скрытие капчи
            HideError();                        // Скрытие ошибки
            AuthorizeUser();                    // Авторизация пользователя
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

        private void AuthorizeUser()
        {
            if (string.IsNullOrWhiteSpace(_pendingLogin) || string.IsNullOrWhiteSpace(_pendingPassword)) // Проверка наличия данных
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
                _credentialsVerified = false;   // Сброс флага проверки
                return;
            }

            string role = GetUserRole(_pendingLogin, _pendingPassword); // Получение роли пользователя
            string name = user.Name;            // Получение имени пользователя
            UserData.CurrentUserId = user.Id;   // Сохранение ID пользователя
            _failedAttempts = 0;                // Сброс счетчика неудачных попыток
            _credentialsVerified = false;       // Сброс флага проверки
            _captchaGraceUntil = null;          // Сброс режима милосердия
            UserData.CurrentUserRole = role;    // Сохранение роли в глобальных данных
            UserData.CurrentUserName = name;    // Сохранение имени в глобальных данных
            OnUserAuthorized?.Invoke();         // Вызов события авторизации
            Manager.MainFrame.Navigate(new MainMenuPage(role)); // Переход на главную страницу
            ResetLoginUI();                     // Сброс интерфейса
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            VerifyCredentials();                // Проверка учетных данных при нажатии кнопки
        }

        private void CaptchaSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            AuthorizeWithCaptcha();             // Авторизация с капчей при押すボタン
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

        private void LoginBox_GotFocus(object sender, RoutedEventArgs e)
        {
            LoginText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на логине
        }

        private void LoginBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text)) // Показ подсказки, если логин пуст
                LoginText.Visibility = Visibility.Visible;
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на пароле
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password)) // Показ подсказки, если пароль пуст
                PasswordText.Visibility = Visibility.Visible;
        }

        private void CaptchaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CaptchaText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на капче
        }

        private void CaptchaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CaptchaTextBox.Text)) // Показ подсказки, если капча пуста
                CaptchaText.Visibility = Visibility.Visible;
        }

        private void CaptchaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)             // Авторизация с капчей по Enter
                AuthorizeWithCaptcha();
        }

        public void ResetAuthorizationState()
        {
            _pendingLogin = null;               // Очистка ожидающего логина
            _pendingPassword = null;            // Очистка ожидающего пароля
            _credentialsVerified = false;       // Сброс флага проверки
            _failedAttempts = 0;                // Сброс счетчика неудач
            ResetLoginUI(true);                 // Полный сброс интерфейса
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок
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
            LoginBox.Clear(); // Очистка поля логина
            PasswordBox.Clear(); // Очистка поля пароля
            UpdatePlaceholderVisibility();      // Обновление видимости подсказок (показ LoginText и PasswordText)

        }
    }
}