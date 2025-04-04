using System;
using System.Collections.Generic;
using System.IO;
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
        public event Action OnRoleChanged;

        public MainMenuPage(string role)
        {
            InitializeComponent();
            _Role = role;

            SetPermissionsBasedOnRole();
            OnRoleChanged?.Invoke();
        }

        private void SetPermissionsBasedOnRole()
        {
            // Установка видимости элементов управления в зависимости от роли пользователя
            switch (_Role)
            {
                case "Администратор":
                    // Администратор видит все элементы управления
                    AdminControlsVisibility(true);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    break;
                case "Делопроизводитель":
                    // Делопроизводитель видит только свои элементы и архивариуса
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(false);
                    break;
                case "Архивариус":
                    // Архивариус видит только свои элементы
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    break;
                default:
                    MessageBox.Show("Неизвестная роль!");
                    break;
            }
        }

        private void AdminControlsVisibility(bool isVisible)
        {
            // Управление видимостью элементов для администратора
            ReportBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            UserBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            RegCardBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            RequestBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClerkControlsVisibility(bool isVisible)
        {
            // Управление видимостью элементов для делопроизводителя
            RegCardBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            DocumentBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            ExpBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

        }

        private void ArchivariusControlsVisibility(bool isVisible)
        {
            // Управление видимостью элементов для архивариуса
            RequestBtn.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DocumentBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new DocumentPage());
        }

        private void UserBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new UserPage());
        }

        private void RegCardBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new RegCardPage());
        }

        private void RequestBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new RequestPage());
        }

        private void ExpBtn_Click(object sender, RoutedEventArgs e)
        {
            // Обработка экспорта данных в Excel
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Экспорт",
                DefaultExt = ".xlsx",
                Filter = "Excel files (.xlsx)|*.xlsx"
            };

            // Отображение диалога сохранения файла
            if (saveFileDialog.ShowDialog() == true)
            {
                // Вызов метода экспорта с указанием пути к файлу
                ExportExcel.ExportToExcel(saveFileDialog.FileName);
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            // Обработка создания отчета в Word
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Отчет",
                DefaultExt = ".docx",
                Filter = "Word documents (.docx)|*.docx"
            };

            // Отображение диалога сохранения файла
            if (saveFileDialog.ShowDialog() == true)
            {
                // Вызов метода экспорта с указанием пути к файлу
                ExportWord.ExportToWord(saveFileDialog.FileName);
            }
        }
    }
}
