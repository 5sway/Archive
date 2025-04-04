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
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isMenuVisible = true;
        private AuthorizePage _authorizePage;

        public MainWindow()
        {
            InitializeComponent();
            Manager.MainFrame = MainFrame;
            _authorizePage = new AuthorizePage();
            _authorizePage.OnUserAuthorized += ShowElements;
            MainFrame.Navigate(_authorizePage);
            HideElements();
            MainFrame.Navigated += MainFrame_Navigated;
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Добавляем небольшую задержку для предотвращения частых обновлений
            await Task.Delay(50);
            if (SearchBox.Text != ((TextBox)sender).Text) return;

            string searchText = SearchBox.Text.Trim().ToLower();
            // Управление видимостью подсказки в поле поиска
            SearchText.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(searchText)) return;

            string currentRole = UserData.CurrentUserRole;

            // Словарь условий поиска и соответствующих действий
            var pageRoutes = new Dictionary<Func<string, bool>, Action>
    {
        // Главное меню
        { t => t.Contains("главн") || t.Contains("меню"),
            () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) },
        
        // Для администратора
        { t => currentRole == "Администратор" && (t.Contains("запрос")),
            () => NavigateIfNotCurrent<RequestPage>() },
        { t => currentRole == "Администратор" && (t.Contains("док") || t.Contains("документ")),
            () => NavigateIfNotCurrent<DocumentPage>() },
        { t => currentRole == "Администратор" && (t.Contains("карт") || t.Contains("регистрац")),
            () => NavigateIfNotCurrent<RegCardPage>() },
        { t => currentRole == "Администратор" && (t.Contains("польз") || t.Contains("user")),
            () => NavigateIfNotCurrent<UserPage>() },
        
        // Для архивариуса
        { t => currentRole == "Архивариус" && (t.Contains("док") || t.Contains("документ")),
            () => NavigateIfNotCurrent<DocumentPage>() },
        { t => currentRole == "Архивариус" && (t.Contains("карт") || t.Contains("регистрац")),
            () => NavigateIfNotCurrent<RegCardPage>() },
        { t => currentRole == "Архивариус" && (t.Contains("запрос")),
            () => NavigateIfNotCurrent<RequestPage>() },
        
        // Для делопроизводителя
        { t => currentRole == "Делопроизводитель" && (t.Contains("док") || t.Contains("документ")),
            () => NavigateIfNotCurrent<DocumentPage>() },
        { t => currentRole == "Делопроизводитель" && (t.Contains("карт") || t.Contains("регистрац")),
            () => NavigateIfNotCurrent<RegCardPage>() },
        
        // Быстрые команды по первым буквам
        { t => t.StartsWith("з") && t.Length > 2 && currentRole == "Администратор" && currentRole == "Архивриус",
            () => NavigateIfNotCurrent<RequestPage>() },
        { t => t.StartsWith("д") && t.Length > 2,
            () => NavigateIfNotCurrent<DocumentPage>() },
        { t => t.StartsWith("к") && t.Length > 2,
            () => NavigateIfNotCurrent<RegCardPage>() },
        { t => t.StartsWith("р") && t.Length > 2,
            () => NavigateIfNotCurrent(() => new MainMenuPage(currentRole)) },
        { t => t.StartsWith("п") && t.Length > 2 && currentRole == "Администратор",
            () => NavigateIfNotCurrent<UserPage>() }
    };

            // Поиск подходящего маршрута и выполнение навигации
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
            // Навигация на страницу, если она еще не открыта
            if (!(MainFrame.Content is T))
                MainFrame.Navigate(new T());
            HighlightActiveButton(); // Обновляем выделение активной кнопки
        }

        private void NavigateIfNotCurrent(Func<Page> pageFactory)
        {
            // Навигация с использованием фабрики страниц
            if (!(MainFrame.Content?.GetType() == pageFactory().GetType()))
                MainFrame.Navigate(pageFactory());
            HighlightActiveButton(); // Обновляем выделение активной кнопки
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
            // Показываем основные элементы интерфейса после авторизации
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

            // Специальные правила видимости для разных ролей
            if (UserData.CurrentUserRole == "Делопроизводитель")
            {
                ReqBtn.Visibility = Visibility.Collapsed; // Скрываем кнопку запросов для делопроизводителя
            }
            else
            {
                ReqBtn.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchText.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchText.Visibility = Visibility.Visible;
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Focus();
            }
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
            // Пропускаем действие если текущая страница - авторизация
            if (MainFrame.Content is AuthorizePage) return;

            // Переключаем состояние меню
            isMenuVisible = !isMenuVisible;

            // Устанавливаем новую видимость элементов меню
            Visibility newVisibility = isMenuVisible ? Visibility.Visible : Visibility.Collapsed;

            // Применяем видимость ко всем элементам меню
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
            // Сбрасываем стили всех кнопок
            ResetButtonStyles();

            // Создаем кисть для выделения активной кнопки
            Brush activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a4d2d9"));

            // Определяем текущую страницу и выделяем соответствующую кнопку
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

        private async void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.No)
                return;
            MainFrame.Navigate(_authorizePage);
            HideElements();
        }
    }
}
