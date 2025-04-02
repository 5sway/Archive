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
        public MainWindow()
        {
            InitializeComponent();
            Manager.MainFrame = MainFrame;
            AuthorizePage authorizePage = new AuthorizePage();
            authorizePage.OnUserAuthorized += ShowElements;
            MainFrame.Navigate(authorizePage);
            HideElements();
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
            RepBtn.Visibility = Visibility.Collapsed;
            MenuGrid.Visibility = Visibility.Collapsed;
            SearchBtn.Visibility = Visibility.Collapsed;
            RefreshBtn.Visibility = Visibility.Collapsed;
            NotBtn.Visibility = Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Collapsed;
            SearchText.Visibility = Visibility.Collapsed;

        }

        private void ShowElements()
        {
            MainBtn.Visibility = Visibility.Visible;
            DocBtn.Visibility = Visibility.Visible;
            ReqBtn.Visibility = Visibility.Visible;
            RepBtn.Visibility = Visibility.Visible;
            MenuGrid.Visibility = Visibility.Visible;
            SearchBtn.Visibility = Visibility.Visible;
            RefreshBtn.Visibility = Visibility.Visible;
            NotBtn.Visibility = Visibility.Visible;
            SearchBox.Visibility = Visibility.Visible;
            SearchText.Visibility = Visibility.Visible;
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
            if (MainFrame.Content is AuthorizePage) return;
            isMenuVisible = !isMenuVisible;

            Visibility newVisibility = isMenuVisible ? Visibility.Visible : Visibility.Collapsed;

            MenuGrid.Visibility = newVisibility;
            DocBtn.Visibility = newVisibility;
            ReqBtn.Visibility = newVisibility;
            RepBtn.Visibility = newVisibility;
            MainBtn.Visibility = newVisibility;
        }

        private void MainBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is MainMenuPage) return;
            string role = UserData.CurrentUserRole;
            MainFrame.Navigate(new MainMenuPage(role));

        }

        private void DocBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DocumentPage());

        }

        private void ReqBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new RequestPage());
        }

        private void RepBtn_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportPage());
        }
    }
}
