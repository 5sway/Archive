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
    /// Логика взаимодействия для MainMenuPage.xaml
    /// </summary>
    public partial class MainMenuPage : Page
    {
        private string _Role;

        public MainMenuPage(string role)
        {
            InitializeComponent();
            _Role = role;

            SetPermissionsBasedOnRole();
        }
        private void SetPermissionsBasedOnRole()
        {
            switch (_Role)
            {
                case "Администратор":
                    AdminControlsVisibility(true);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    break;
                case "Делопроизводитель":
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(false);
                    break;
                case "Архивариус":
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(false);
                    ArchivariusControlsVisibility(true);
                    break;
                default:
                    MessageBox.Show("Неизвестная роль!");
                    break;
            }
        }

        private void AdminControlsVisibility(bool isVisible)
        {
            AdminBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClerkControlsVisibility(bool isVisible)
        {
            ClerkBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ArchivariusControlsVisibility(bool isVisible)
        {
            ArchivariusBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
