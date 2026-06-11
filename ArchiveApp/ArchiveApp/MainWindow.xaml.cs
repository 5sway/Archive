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
using System.Windows.Media.Animation;

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
        private bool isMenuVisible = true;           // Флаг видимости меню
        private AuthorizePage _authorizePage;        // Страница авторизации
        private System.Timers.Timer _highPrecisionTimer; // Высокоточный таймер для времени
        private string currentUserRole = UserData.CurrentUserRole;

        public MainWindow()
        {
            InitializeComponent();                    // Инициализация компонентов окна
            timeBeginPeriod(1);                      // Установка высокой точности таймера
            Manager.MainFrame = MainFrame;           // Назначение главного фрейма для навигации
            _authorizePage = new AuthorizePage();    // Создание страницы авторизации
            _authorizePage.OnUserAuthorized += ShowElements; // Подписка на событие успешной авторизации
            MainFrame.Navigate(_authorizePage);     // Переход на страницу авторизации
            HideElements();                          // Скрытие элементов интерфейса до авторизации
            MainFrame.Navigated += MainFrame_Navigated; // Подписка на событие смены страницы
            Closed += (s, e) => timeEndPeriod(1);    // Отключение высокой точности таймера при закрытии окна
            MouseDown += Window_MouseDown;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchTextVisibility();
        }

        private void UpdateSearchTextVisibility()
        {
            if (MainFrame.Content is AuthorizePage) return;

            // Подсказка видна, если SearchBox пуст
            SearchText.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, находимся ли мы на странице авторизации
            if (MainFrame.Content is AuthorizePage) return;

            // Проверяем, был ли клик вне SearchBox
            if (!IsMouseOverSearchBox(e.GetPosition(this)))
            {
                ClearSearchBox();
            }
        }

        private bool IsMouseOverSearchBox(Point position)
        {
            // Проверяем, находится ли точка в пределах SearchBox или SearchText
            var searchBoxBounds = new Rect(
                SearchBox.TranslatePoint(new Point(0, 0), this),
                new Size(SearchBox.ActualWidth, SearchBox.ActualHeight));

            var searchTextBounds = new Rect(
                SearchText.TranslatePoint(new Point(0, 0), this),
                new Size(SearchText.ActualWidth, SearchText.ActualHeight));

            return searchBoxBounds.Contains(position) || searchTextBounds.Contains(position);
        }

        private void ClearSearchBox()
        {
            // Проверяем, находимся ли мы на странице авторизации
            if (MainFrame.Content is AuthorizePage) return;

            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = "";
            }

            UpdateSearchTextVisibility();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(50);                    // Задержка для предотвращения частых обновлений поиска
            if (SearchBox.Text != ((TextBox)sender).Text) return; // Проверка актуальности текста

            UpdateSearchTextVisibility();

            string searchText = SearchBox.Text.Trim().ToLower(); // Получение текста поиска в нижнем регистре
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
                { t => t.StartsWith("п") && t.Length > 2 && currentRole == "Администратор", () => NavigateIfNotCurrent<UserPage>() },
                { t => t.StartsWith("о") && t.Length > 2 && currentRole == "Администратор", () => NavigateToReportOptions(true, currentRole) },
                { t => t.StartsWith("о") && t.Length > 2, () => NavigateToReportOptions(false, currentRole) }
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

        private void NavigateToReportOptions(bool isFullReport, string role)
        {
            if (MainFrame.Content?.GetType() != typeof(ReportOptionsPage))
            {
                var optionsPage = new ReportOptionsPage(isFullReport, role);
                var mainMenuPage = new MainMenuPage(role);
                optionsPage.ReportOptionsSelected += (format, isTableFormat, tables, selectedRecordIds, startDate, endDate) =>
                {
                    mainMenuPage.GetType()
                        .GetMethod("HandleReportOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(mainMenuPage, new object[] { format, isTableFormat, tables, selectedRecordIds, startDate, endDate, role, isFullReport ? "Отчет" : "Простой отчет" });
                };
                MainFrame.Navigate(optionsPage);
            }
            HighlightActiveButton();
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is AuthorizePage)
            {
                HideElements();
            }
            else
            {
                ShowElements();
                // Проверяем роль пользователя и скрываем кнопки для "Делопроизводитель"
                string role = UserData.CurrentUserRole;
                if (role == "Делопроизводитель")
                {
                    ReqBtn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ReqBtn.Visibility = Visibility.Visible;
                }
            }
            HighlightActiveButton();
            UpdateSearchTextVisibility();
            Keyboard.ClearFocus(); // Сбрасываем фокус после навигации
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
            RepBtn.Visibility = Visibility.Collapsed;
            SearchBox.Text = "";
            Keyboard.ClearFocus();
            MenuGrid.ClearValue(FrameworkElement.FocusVisualStyleProperty);
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
            RepBtn.Visibility = Visibility.Visible;
            string role = UserData.CurrentUserRole;
            if (role == "Делопроизводитель")
            {
                ReqBtn.Visibility = Visibility.Collapsed;
            }

            UpdateSearchTextVisibility();
            SearchBox.Text = "";

            // Сбрасываем фокус
            Keyboard.ClearFocus();
            MenuGrid.ClearValue(FrameworkElement.FocusVisualStyleProperty);
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                SearchBox.Focus();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Запускаем анимацию вращения кнопки
            var rotateAnimation = (Storyboard)Resources["RotateAnimation"];
            Storyboard.SetTarget(rotateAnimation, RefreshBtn);
            rotateAnimation.Begin();

            // Запускаем анимацию затемнения и восстановления
            var reloadAnimation = (Storyboard)Resources["ReloadAnimation"];
            Storyboard.SetTarget(reloadAnimation, MainGrid);
            reloadAnimation.Completed += (s, args) =>
            {
                // После завершения анимации обновляем данные и сбрасываем состояние
                UpdateData();
                ResetAppState();
            };
            reloadAnimation.Begin();
        }

        private void ResetAppState()
        {
            // Сбрасываем состояние приложения
            string role = UserData.CurrentUserRole; // Переходим на главную страницу
            SearchBox.Text = ""; // Очищаем поле поиска
            UpdateSearchTextVisibility();
            isMenuVisible = true; // Показываем меню
            MenuGrid.Visibility = Visibility.Visible;
            DocBtn.Visibility = Visibility.Visible;
            ReqBtn.Visibility = UserData.CurrentUserRole == "Делопроизводитель" ? Visibility.Collapsed : Visibility.Visible;
            CardBtn.Visibility = Visibility.Visible;
            MainBtn.Visibility = Visibility.Visible;
            ExitBtn.Visibility = Visibility.Visible;
            HighlightActiveButton(); // Обновляем подсветку активной кнопки
            CloseNotificationPopup(); // Закрываем уведомление, если оно открыто
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

            UpdateSearchTextVisibility();
        }

        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is MainMenuPage) return;
            string role = UserData.CurrentUserRole;
            MainFrame.Navigate(new MainMenuPage(role));
            HighlightActiveButton();
            UpdateSearchTextVisibility();
        }

        private void DocBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DocumentPage());
            HighlightActiveButton();
            UpdateSearchTextVisibility();
        }

        private void ReqBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RequestPage());
            HighlightActiveButton();
            UpdateSearchTextVisibility();
        }

        private void CardBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RegCardPage());
            HighlightActiveButton();
            UpdateSearchTextVisibility();
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

            UpdateSearchTextVisibility();
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

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Закрытие уведомления, если оно открыто
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

            // Проверяем, находимся ли мы на странице авторизации
            if (MainFrame.Content is AuthorizePage) return;

            // Получаем элемент, на который был произведён клик
            var clickedElement = e.OriginalSource as DependencyObject;

            // Проверяем, является ли клик по корневому Grid (MainGrid)
            bool isEmptySpace = false;
            while (clickedElement != null)
            {
                if (clickedElement is Grid grid && grid.Name == "MainGrid")
                {
                    isEmptySpace = true;
                    break;
                }
                // Игнорируем клики по другим элементам (например, Button, TextBox, TextBlock)
                if (clickedElement is Button || clickedElement is TextBox ||
                    clickedElement is TextBlock || clickedElement is Image ||
                    clickedElement is Frame || clickedElement is Border)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // Если клик был на пустом месте и SearchBox в фокусе, снимаем фокус
            if (isEmptySpace && Keyboard.FocusedElement == SearchBox)
            {
                Keyboard.ClearFocus();
                UpdateSearchTextVisibility();
            }
        }

        private void SearchText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SearchBox.Focus();                           // Перевод фокуса на поле поиска
        }

        private void RepBtn_Click(object sender, RoutedEventArgs e)
        {
            string userRole = UserData.CurrentUserRole;
            bool isFullReport = userRole == "Администратор";
            NavigateToReportOptions(isFullReport, userRole);
            UpdateSearchTextVisibility();
        }

        private void MainFrame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, находимся ли мы на странице авторизации
            if (MainFrame.Content is AuthorizePage) return;

            // Получаем элемент, на который был произведён клик
            var clickedElement = e.OriginalSource as DependencyObject;

            // Проверяем, является ли клик по корневому элементу страницы (обычно Grid)
            bool isEmptySpace = false;
            while (clickedElement != null)
            {
                // Если клик был на корневом Grid страницы (без имени, чтобы исключить вложенные Grid)
                if (clickedElement is Grid grid && string.IsNullOrEmpty(grid.Name))
                {
                    isEmptySpace = true;
                    break;
                }
                // Игнорируем клики по другим элементам (например, Button, TextBox, TextBlock)
                if (clickedElement is Button || clickedElement is TextBox ||
                    clickedElement is TextBlock || clickedElement is Image ||
                    clickedElement is Border || clickedElement is DataGrid ||
                    clickedElement is ListBox || clickedElement is ComboBox)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // Если клик был на пустом месте и SearchBox в фокусе, снимаем фокус
            if (isEmptySpace && Keyboard.FocusedElement == SearchBox)
            {
                Keyboard.ClearFocus();
                UpdateSearchTextVisibility();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Esc или Enter
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // Если один из DatePicker в фокусе, снимаем фокус
                if (Keyboard.FocusedElement == SearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
            }
        }
    }
}