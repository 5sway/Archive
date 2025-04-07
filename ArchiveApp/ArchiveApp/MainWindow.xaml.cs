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
        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);
        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        private Border _notificationPopup;           // Контейнер для всплывающего уведомления
        private bool _isNotificationVisible;         // Флаг видимости уведомления
        private DispatcherTimer _updateTimer;        // Таймер обновления размера окна
        private TextBlock _windowSizeText;           // Текст с размерами окна
        private TextBlock _timeText;                 // Текст с текущим временем
        private TextBlock _userNameText;             // Текст с именем пользователя
        private TextBlock _userRoleText;             // Текст с ролью пользователя
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
            MainGrid.MouseDown += MainGrid_MouseDown; // Подписка на событие клика по пустому месту
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(50);                    // Задержка для предотвращения частых обновлений поиска
            if (SearchBox.Text != ((TextBox)sender).Text) return; // Проверка актуальности текста

            string searchText = SearchBox.Text.Trim().ToLower(); // Получение текста поиска в нижнем регистре
            SearchText.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed; // Управление видимостью подсказки поиска

            if (string.IsNullOrEmpty(searchText)) return; // Выход, если поиск пустой

            string currentRole = UserData.CurrentUserRole; // Текущая роль пользователя

            var pageRoutes = new Dictionary<Func<string, bool>, Action>
            {
                { t => t.Contains("главн") || t.Contains("меню"), () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) },
                { t => currentRole == "Администратор" && t.Contains("запрос"), () => NavigateIfNotCurrent<RequestPage>() },
                { t => currentRole == "Администратор" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() },
                { t => currentRole == "Администратор" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() },
                { t => currentRole == "Администратор" && (t.Contains("польз") || t.Contains("user")), () => NavigateIfNotCurrent<UserPage>() },
                { t => currentRole == "Архивариус" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() },
                { t => currentRole == "Архивариус" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() },
                { t => currentRole == "Архивариус" && t.Contains("запрос"), () => NavigateIfNotCurrent<RequestPage>() },
                { t => currentRole == "Делопроизводитель" && (t.Contains("док") || t.Contains("документ")), () => NavigateIfNotCurrent<DocumentPage>() },
                { t => currentRole == "Делопроизводитель" && (t.Contains("карт") || t.Contains("регистрац")), () => NavigateIfNotCurrent<RegCardPage>() },
                { t => t.StartsWith("з") && t.Length > 2 && (currentRole == "Администратор" || currentRole == "Архивариус"), () => NavigateIfNotCurrent<RequestPage>() },
                { t => t.StartsWith("д") && t.Length > 2, () => NavigateIfNotCurrent<DocumentPage>() },
                { t => t.StartsWith("к") && t.Length > 2, () => NavigateIfNotCurrent<RegCardPage>() },
                { t => t.StartsWith("р") && t.Length > 2, () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) },
                { t => t.StartsWith("п") && t.Length > 2 && currentRole == "Администратор", () => NavigateIfNotCurrent<UserPage>() }
            };

            foreach (var route in pageRoutes)
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
            if (!(MainFrame.Content is T))
                MainFrame.Navigate(new T());
            HighlightActiveButton();
        }

        private void NavigateIfNotCurrent(Func<Page> pageFactory)
        {
            if (!(MainFrame.Content?.GetType() == pageFactory().GetType()))
                MainFrame.Navigate(pageFactory());
            HighlightActiveButton();
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            HighlightActiveButton();
        }

        public void Initialize(AuthorizePage authorizePage)
        {
            authorizePage.OnUserAuthorized += ShowElements;
        }

        private void HideElements()
        {
            MainBtn.Visibility = Visibility.Collapsed;
            DocBtn.Visibility = Visibility.Collapsed;
            ReqBtn.Visibility = Visibility.Collapsed;
            CardBtn.Visibility = Visibility.Collapsed;
            MenuGrid.Visibility = Visibility.Collapsed;
            SearchBtn.Visibility = Visibility.Collapsed;
            RefreshBtn.Visibility = Visibility.Collapsed;
            NotBtn.Visibility = Visibility.Collapsed;
            ExitBtn.Visibility = Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Collapsed;
            SearchText.Visibility = Visibility.Collapsed;
            SearchBox.Text = "";
        }

        private void ShowElements()
        {
            MainBtn.Visibility = Visibility.Visible;
            DocBtn.Visibility = Visibility.Visible;
            ReqBtn.Visibility = Visibility.Visible;
            CardBtn.Visibility = Visibility.Visible;
            MenuGrid.Visibility = Visibility.Visible;
            SearchBtn.Visibility = Visibility.Visible;
            RefreshBtn.Visibility = Visibility.Visible;
            NotBtn.Visibility = Visibility.Visible;
            ExitBtn.Visibility = Visibility.Visible;
            SearchBox.Visibility = Visibility.Visible;
            SearchText.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            SearchBox.Text = "";

            if (UserData.CurrentUserRole == "Делопроизводитель")
                ReqBtn.Visibility = Visibility.Collapsed;
            else
                ReqBtn.Visibility = Visibility.Visible;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchText.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                SearchText.Visibility = Visibility.Visible;
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                SearchBox.Focus();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateData();
        }

        private void UpdateData()
        {
            var updatedData = ArchiveBaseEntities.GetContext().User.ToList();
        }

        private void BurgerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is AuthorizePage) return;

            isMenuVisible = !isMenuVisible;
            Visibility newVisibility = isMenuVisible ? Visibility.Visible : Visibility.Collapsed;

            MenuGrid.Visibility = newVisibility;
            DocBtn.Visibility = newVisibility;
            ReqBtn.Visibility = newVisibility;
            CardBtn.Visibility = newVisibility;
            MainBtn.Visibility = newVisibility;
            ExitBtn.Visibility = newVisibility;
        }

        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is MainMenuPage) return;
            string role = UserData.CurrentUserRole;
            MainFrame.Navigate(new MainMenuPage(role));
            HighlightActiveButton();
        }

        private void DocBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DocumentPage());
            HighlightActiveButton();
        }

        private void ReqBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RequestPage());
            HighlightActiveButton();
        }

        private void CardBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RegCardPage());
            HighlightActiveButton();
        }

        private void HighlightActiveButton()
        {
            ResetButtonStyles();
            Brush activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a4d2d9"));

            if (MainFrame.Content is MainMenuPage)
                MainBtn.Background = activeBrush;
            else if (MainFrame.Content is DocumentPage)
                DocBtn.Background = activeBrush;
            else if (MainFrame.Content is RequestPage)
                ReqBtn.Background = activeBrush;
            else if (MainFrame.Content is RegCardPage)
                CardBtn.Background = activeBrush;
        }

        private void ResetButtonStyles()
        {
            Brush defaultBrush = Brushes.Transparent;
            MainBtn.Background = defaultBrush;
            DocBtn.Background = defaultBrush;
            ReqBtn.Background = defaultBrush;
            CardBtn.Background = defaultBrush;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Подтверждение выхода", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No) return;

            CloseNotificationPopup();                 // Закрытие всплывающего окна перед выходом
            _authorizePage.ResetAuthorizationState();
            MainFrame.Navigate(_authorizePage);
            HideElements();
        }

        private void InitializeNotificationPopup()
        {
            _notificationPopup = new Border
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

            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Информация о системе",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stackPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 10) });

            var userNameRow = CreateInfoRow("Пользователь:", "");
            _userNameText = (TextBlock)userNameRow.Children[1];
            stackPanel.Children.Add(userNameRow);

            var userRoleRow = CreateInfoRow("Роль:", "");
            _userRoleText = (TextBlock)userRoleRow.Children[1];
            stackPanel.Children.Add(userRoleRow);

            var sizeRow = CreateInfoRow("Размер окна:", "");
            _windowSizeText = (TextBlock)sizeRow.Children[1];
            stackPanel.Children.Add(sizeRow);

            var timeRow = CreateInfoRow("Время:", "");
            _timeText = (TextBlock)timeRow.Children[1];
            stackPanel.Children.Add(timeRow);

            _notificationPopup.Child = stackPanel;

            _highPrecisionTimer = new System.Timers.Timer(1000);
            _highPrecisionTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_isNotificationVisible)
                        _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
                });
            };

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            _updateTimer.Tick += (s, e) =>
            {
                if (_isNotificationVisible && IsActive)
                    _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px";
            };

            Panel.SetZIndex(_notificationPopup, 100);
            Grid.SetColumn(_notificationPopup, 1);
            Grid.SetRow(_notificationPopup, 1);
            MainGrid.Children.Add(_notificationPopup);
        }

        private void NotBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_notificationPopup == null)
                InitializeNotificationPopup();

            if (_isNotificationVisible)
            {
                CloseNotificationPopup();
            }
            else
            {
                var buttonRight = NotBtn.TranslatePoint(new Point(NotBtn.ActualWidth, 0), MainGrid).X;
                var popupLeft = buttonRight - _notificationPopup.Width;

                Canvas.SetLeft(_notificationPopup, popupLeft);
                Canvas.SetTop(_notificationPopup, NotBtn.ActualHeight + 5);

                _userNameText.Text = !string.IsNullOrEmpty(UserData.CurrentUserName) ? UserData.CurrentUserName : "Гость";
                _userRoleText.Text = !string.IsNullOrEmpty(UserData.CurrentUserRole) ? UserData.CurrentUserRole : "Не определено";
                _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
                _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px";

                _highPrecisionTimer.Start();
                _updateTimer.Start();
                _notificationPopup.Visibility = Visibility.Visible;
                _isNotificationVisible = true;
                SizeChanged += MainWindow_SizeChanged;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isNotificationVisible && IsActive)
                _windowSizeText.Text = $"{Math.Round(ActualWidth)} x {Math.Round(ActualHeight)} px";
        }

        private StackPanel CreateInfoRow(string label, string value)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
                Children =
                {
                    new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Width = 100 },
                    new TextBlock { Text = value }
                }
            };
        }

        // Новый метод для закрытия всплывающего окна
        private void CloseNotificationPopup()
        {
            if (_isNotificationVisible)
            {
                _highPrecisionTimer.Stop();            // Остановка таймера времени
                _updateTimer.Stop();                   // Остановка таймера размера
                _notificationPopup.Visibility = Visibility.Collapsed; // Скрытие окна
                _isNotificationVisible = false;        // Обновление флага
                SizeChanged -= MainWindow_SizeChanged; // Отписка от события изменения размера
            }
        }

        // Обработчик клика по пустому месту
        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isNotificationVisible)
            {
                var popupPosition = _notificationPopup.TransformToAncestor(MainGrid).Transform(new Point(0, 0));
                var popupRect = new Rect(popupPosition, new Size(_notificationPopup.ActualWidth, _notificationPopup.ActualHeight));
                var clickPoint = e.GetPosition(MainGrid);

                // Проверяем, был ли клик вне области уведомления
                if (!popupRect.Contains(clickPoint) && e.OriginalSource != NotBtn)
                {
                    CloseNotificationPopup();
                }
            }
        }
    }
}