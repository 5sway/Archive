using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Timers;

namespace ArchiveApp
{
    public partial class MainWindow : Window
    {
        // Импорт функций для повышения точности таймера Windows
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        private Border _notificationPopup;           // Контейнер для всплывающего уведомления
        private bool _isNotificationVisible;         // Флаг видимости уведомления
        private DispatcherTimer _updateTimer;        // Таймер обновления размера окна
        private TextBlock _windowSizeText;           // Текст с размерами окна
        private TextBlock _timeText;                 // Текст с текущим временем
        private string currentUserName = UserData.CurrentUserName; // Имя текущего пользователя
        private bool isMenuVisible = true;           // Флаг видимости меню
        private AuthorizePage _authorizePage;        // Страница авторизации
        private bool _isWindowActive = true;         // Флаг активности окна
        private System.Timers.Timer _highPrecisionTimer; // Высокоточный таймер для времени

        public MainWindow()
        {
            InitializeComponent();                    // Инициализация компонентов окна
            timeBeginPeriod(1);                      // Установка высокой точности таймера
            Manager.MainFrame = MainFrame;           // Назначение главного фрейма для навигации
            _authorizePage = new AuthorizePage();    // Создание страницы авторизации
            _authorizePage.OnUserAuthorized += ShowElements; // Подписка на событие успешной авторизации
            MainFrame.Navigate(_authorizePage);      // Переход на страницу авторизации
            HideElements();                          // Скрытие элементов интерфейса до авторизации
            MainFrame.Navigated += MainFrame_Navigated; // Подписка на событие смены страницы
            Closed += (s, e) => timeEndPeriod(1);    // Отключение высокой точности таймера при закрытии окна
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(50);                    // Задержка для предотвращения частых обновлений поиска
            if (SearchBox.Text != ((TextBox)sender).Text) return; // Проверка актуальности текста

            string searchText = SearchBox.Text.Trim().ToLower(); // Получение текста поиска в нижнем регистре
            SearchText.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed; // Управление видимостью подсказки поиска

            if (string.IsNullOrEmpty(searchText)) return; // Выход, если поиск пустой

            string currentRole = UserData.CurrentUserRole; // Текущая роль пользователя

            // Словарь для маршрутизации поиска по ключевым словам и ролям
            var pageRoutes = new Dictionary<Func<string, bool>, Action>
            {
                { t => t.Contains("главн") || t.Contains("меню"), () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) }, // Главное меню
                { t => currentRole == "Администратор" && t.Contains("запрос"), () => NavigateIfNotCurrent<RequestPage>() }, // Запросы для админа
                { t => currentRole == "Администратор" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() }, // Документы для админа
                { t => currentRole == "Администратор" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() }, // Карточки для админа
                { t => currentRole == "Администратор" && (t.Contains("польз") || t.Contains("user")), () => NavigateIfNotCurrent<UserPage>() }, // Пользователи для админа
                { t => currentRole == "Архивариус" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() }, // Документы для архивариуса
                { t => currentRole == "Архивариус" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() }, // Карточки для архивариуса
                { t => currentRole == "Архивариус" && t.Contains("запрос"), () => NavigateIfNotCurrent<RequestPage>() }, // Запросы для архивариуса
                { t => currentRole == "Делопроизводитель" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() }, // Документы для делопроизводителя
                { t => currentRole == "Делопроизводитель" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() }, // Карточки для делопроизводителя
                { t => t.StartsWith("з") && t.Length > 2 && (currentRole == "Администратор" || currentRole == "Архивариус"), () => NavigateIfNotCurrent<RequestPage>() }, // Быстрый переход на запросы
                { t => t.StartsWith("д") && t.Length > 2, () => NavigateIfNotCurrent<DocumentPage>() }, // Быстрый переход на документы
                { t => t.StartsWith("к") && t.Length > 2, () => NavigateIfNotCurrent<RegCardPage>() }, // Быстрый переход на карточки
                { t => t.StartsWith("р") && t.Length > 2, () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) }, // Быстрый переход на меню
                { t => t.StartsWith("п") && t.Length > 2 && currentRole == "Администратор", () => NavigateIfNotCurrent<UserPage>() } // Быстрый переход на пользователей
            };

            foreach (var route in pageRoutes)        // Поиск подходящего маршрута и выполнение навигации
            {
                if (route.Key(searchText))
                {
                    route.Value();
                    break;
                }
            }
        }

        private void NavigateIfNotCurrent<T>() where T : Page, new()
        {
            if (!(MainFrame.Content is T))           // Проверка, не является ли текущая страница целевой
                MainFrame.Navigate(new T());         // Навигация на новую страницу
            HighlightActiveButton();                // Обновление выделения кнопки
        }

        private void NavigateIfNotCurrent(Func<Page> pageFactory)
        {
            if (!(MainFrame.Content?.GetType() == pageFactory().GetType())) // Проверка типа текущей страницы
                MainFrame.Navigate(pageFactory());  // Навигация с использованием фабрики
            HighlightActiveButton();                // Обновление выделения кнопки
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            HighlightActiveButton();                // Выделение активной кнопки после навигации
        }

        public void Initialize(AuthorizePage authorizePage)
        {
            authorizePage.OnUserAuthorized += ShowElements; // Подписка на событие авторизации для внешней инициализации
        }

        private void HideElements()
        {
            MainBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки главного меню
            DocBtn.Visibility = Visibility.Collapsed;  // Скрытие кнопки документов
            ReqBtn.Visibility = Visibility.Collapsed;  // Скрытие кнопки запросов
            CardBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки карточек
            MenuGrid.Visibility = Visibility.Collapsed;// Скрытие панели меню
            SearchBtn.Visibility = Visibility.Collapsed;// Скрытие кнопки поиска
            RefreshBtn.Visibility = Visibility.Collapsed;// Скрытие кнопки обновления
            NotBtn.Visibility = Visibility.Collapsed;  // Скрытие кнопки уведомлений
            ExitBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки выхода
            SearchBox.Visibility = Visibility.Collapsed;// Скрытие поля поиска
            SearchText.Visibility = Visibility.Collapsed;// Скрытие подсказки поиска
            SearchBox.Text = "";                       // Очистка текста поиска
        }

        private void ShowElements()
        {
            MainBtn.Visibility = Visibility.Visible;   // Показ кнопки главного меню
            DocBtn.Visibility = Visibility.Visible;    // Показ кнопки документов
            ReqBtn.Visibility = Visibility.Visible;    // Показ кнопки запросов
            CardBtn.Visibility = Visibility.Visible;   // Показ кнопки карточек
            MenuGrid.Visibility = Visibility.Visible;  // Показ панели меню
            SearchBtn.Visibility = Visibility.Visible; // Показ кнопки поиска
            RefreshBtn.Visibility = Visibility.Visible;// Показ кнопки обновления
            NotBtn.Visibility = Visibility.Visible;    // Показ кнопки уведомлений
            ExitBtn.Visibility = Visibility.Visible;   // Показ кнопки выхода
            SearchBox.Visibility = Visibility.Visible; // Показ поля поиска
            SearchText.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed; // Управление подсказкой поиска
            SearchBox.Text = "";                       // Очистка текста поиска

            if (UserData.CurrentUserRole == "Делопроизводитель") // Скрытие кнопки запросов для делопроизводителя
                ReqBtn.Visibility = Visibility.Collapsed;
            else
                ReqBtn.Visibility = Visibility.Visible; // Показ кнопки запросов для других ролей
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchText.Visibility = Visibility.Collapsed; // Скрытие подсказки при фокусе на поле поиска
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) // Показ подсказки, если поле поиска пустое
                SearchText.Visibility = Visibility.Visible;
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) // Фокусировка на поле поиска, если оно пустое
                SearchBox.Focus();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateData();                              // Вызов обновления данных
        }

        private void UpdateData()
        {
            var updatedData = ArchiveBaseEntities.GetContext().User.ToList(); // Обновление данных пользователей
        }

        private void BurgerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is AuthorizePage) return; // Пропуск действия на странице авторизации

            isMenuVisible = !isMenuVisible;            // Переключение состояния видимости меню
            Visibility newVisibility = isMenuVisible ? Visibility.Visible : Visibility.Collapsed; // Новое состояние видимости

            MenuGrid.Visibility = newVisibility;       // Применение видимости к панели меню
            DocBtn.Visibility = newVisibility;         // Применение видимости к кнопке документов
            ReqBtn.Visibility = newVisibility;         // Применение видимости к кнопке запросов
            CardBtn.Visibility = newVisibility;        // Применение видимости к кнопке карточек
            MainBtn.Visibility = newVisibility;        // Применение видимости к кнопке главного меню
            ExitBtn.Visibility = newVisibility;        // Применение видимости к кнопке выхода
        }

        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is MainMenuPage) return; // Пропуск, если уже на главной странице
            string role = UserData.CurrentUserRole;    // Получение роли пользователя
            MainFrame.Navigate(new MainMenuPage(role)); // Навигация на главную страницу
            HighlightActiveButton();                   // Выделение активной кнопки
        }

        private void DocBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DocumentPage());    // Навигация на страницу документов
            HighlightActiveButton();                   // Выделение активной кнопки
        }

        private void ReqBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RequestPage());     // Навигация на страницу запросов
            HighlightActiveButton();                   // Выделение активной кнопки
        }

        private void CardBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RegCardPage());     // Навигация на страницу карточек
            HighlightActiveButton();                   // Выделение активной кнопки
        }

        private void HighlightActiveButton()
        {
            ResetButtonStyles();                       // Сброс стилей всех кнопок
            Brush activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a4d2d9")); // Цвет выделения

            if (MainFrame.Content is MainMenuPage)     // Выделение кнопки главного меню
                MainBtn.Background = activeBrush;
            else if (MainFrame.Content is DocumentPage)// Выделение кнопки документов
                DocBtn.Background = activeBrush;
            else if (MainFrame.Content is RequestPage) // Выделение кнопки запросов
                ReqBtn.Background = activeBrush;
            else if (MainFrame.Content is RegCardPage) // Выделение кнопки карточек
                CardBtn.Background = activeBrush;
        }

        private void ResetButtonStyles()
        {
            Brush defaultBrush = Brushes.Transparent;  // Цвет по умолчанию для кнопок
            MainBtn.Background = defaultBrush;         // Сброс цвета кнопки главного меню
            DocBtn.Background = defaultBrush;          // Сброс цвета кнопки документов
            ReqBtn.Background = defaultBrush;          // Сброс цвета кнопки запросов
            CardBtn.Background = defaultBrush;         // Сброс цвета кнопки карточек
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question); // Подтверждение выхода
            if (result == MessageBoxResult.No) return; // Отмена выхода

            _authorizePage.ResetAuthorizationState();  // Сброс состояния авторизации
            MainFrame.Navigate(_authorizePage);        // Возврат на страницу авторизации
            HideElements();                            // Скрытие элементов интерфейса
        }

        private void InitializeNotificationPopup()
        {
            _notificationPopup = new Border            // Создание всплывающего окна уведомлений
            {
                Width = 280,
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Effect = new DropShadowEffect { Color = Colors.Gray, Direction = 270, ShadowDepth = 5, Opacity = 0.5 },
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 10, 0),
                Visibility = Visibility.Collapsed
            };

            var stackPanel = new StackPanel();         // Контейнер для содержимого уведомления

            stackPanel.Children.Add(new TextBlock      // Заголовок уведомления
            {
                Text = "Информация о системе",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stackPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 10) }); // Разделитель

            string userName = !string.IsNullOrEmpty(UserData.CurrentUserName) ? UserData.CurrentUserName : "Гость"; // Имя пользователя или "Гость"
            string userRole = !string.IsNullOrEmpty(UserData.CurrentUserRole) ? UserData.CurrentUserRole : "Не определено"; // Роль пользователя или "Не определено"

            stackPanel.Children.Add(CreateInfoRow("Пользователь:", userName)); // Строка с именем
            stackPanel.Children.Add(CreateInfoRow("Роль:", userRole));         // Строка с ролью

            var sizeRow = CreateInfoRow("Размер окна:", ""); // Строка с размером окна
            _windowSizeText = (TextBlock)sizeRow.Children[1];
            stackPanel.Children.Add(sizeRow);

            var timeRow = CreateInfoRow("Время:", ""); // Строка с временем
            _timeText = (TextBlock)timeRow.Children[1];
            stackPanel.Children.Add(timeRow);

            _notificationPopup.Child = stackPanel;     // Установка содержимого в уведомление

            _highPrecisionTimer = new System.Timers.Timer(1000); // Таймер для обновления времени каждую секунду
            _highPrecisionTimer.Elapsed += (s, e) =>   // Обновление времени в UI
            {
                Dispatcher.Invoke(() =>
                {
                    if (_isNotificationVisible)
                        _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
                });
            };

            _updateTimer = new DispatcherTimer         // Таймер для обновления размера окна
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            _updateTimer.Tick += (s, e) =>             // Обновление размера окна при активности
            {
                if (_isNotificationVisible && IsActive)
                    _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px";
            };

            Panel.SetZIndex(_notificationPopup, 100);  // Установка высокого Z-индекса для уведомления
            Grid.SetColumn(_notificationPopup, 1);     // Размещение в колонке 1
            Grid.SetRow(_notificationPopup, 1);        // Размещение в строке 1
            MainGrid.Children.Add(_notificationPopup); // Добавление уведомления в главную сетку
        }

        private void NotBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationPopup == null)            // Инициализация уведомления при первом вызове
                InitializeNotificationPopup();

            if (_isNotificationVisible)                // Скрытие уведомления
            {
                _highPrecisionTimer.Stop();            // Остановка таймера времени
                _updateTimer.Stop();                   // Остановка таймера размера
                _notificationPopup.Visibility = Visibility.Collapsed; // Скрытие окна
                _isNotificationVisible = false;        // Обновление флага
                SizeChanged -= MainWindow_SizeChanged; // Отписка от события изменения размера
            }
            else                                       // Показ уведомления
            {
                var buttonRight = NotBtn.TranslatePoint(new Point(NotBtn.ActualWidth, 0), MainGrid).X; // Позиция кнопки
                var popupLeft = buttonRight - _notificationPopup.Width; // Расчет позиции уведомления

                Canvas.SetLeft(_notificationPopup, popupLeft); // Установка горизонтальной позиции
                Canvas.SetTop(_notificationPopup, NotBtn.ActualHeight + 5); // Установка вертикальной позиции

                _timeText.Text = DateTime.Now.ToString("HH:mm:ss"); // Установка текущего времени
                _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px"; // Установка размера окна

                _highPrecisionTimer.Start();           // Запуск таймера времени
                _updateTimer.Start();                  // Запуск таймера размера
                _notificationPopup.Visibility = Visibility.Visible; // Показ окна
                _isNotificationVisible = true;         // Обновление флага
                SizeChanged += MainWindow_SizeChanged; // Подписка на событие изменения размера
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isNotificationVisible && IsActive)    // Обновление размера окна при активности и видимости уведомления
                _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px";
        }

        private StackPanel CreateInfoRow(string label, string value)
        {
            return new StackPanel                      // Создание строки информации для уведомления
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                Children =
                {
                    new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Width = 100 }, // Метка
                    new TextBlock { Text = value }     // Значение
                }
            };
        }
    }
}