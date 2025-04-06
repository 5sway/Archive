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
    public partial class MainMenuPage : Page
    {
        private string _Role;                    // Роль текущего пользователя
        public event Action OnRoleChanged;      // Событие изменения роли

        public MainMenuPage(string role)
        {
            InitializeComponent();               // Инициализация компонентов страницы
            _Role = role;                       // Установка роли пользователя
            SetPermissionsBasedOnRole();        // Настройка видимости элементов по роли
            OnRoleChanged?.Invoke();            // Вызов события изменения роли
        }

        private void SetPermissionsBasedOnRole()
        {
            switch (_Role)                      // Настройка интерфейса в зависимости от роли
            {
                case "Администратор":           // Полный доступ для администратора
                    AdminControlsVisibility(true);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    break;
                case "Делопроизводитель":       // Ограниченный доступ для делопроизводителя
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(false);
                    break;
                case "Архивариус":              // Ограниченный доступ для архивариуса
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    break;
                default:                        // Обработка неизвестной роли
                    MessageBox.Show("Неизвестная роль!");
                    break;
            }
        }

        private void AdminControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed; // Установка видимости
            ReportBtn.Visibility = visibility;  // Кнопка отчета
            UserBtn.Visibility = visibility;    // Кнопка пользователей
            RegCardBtn.Visibility = visibility; // Кнопка карточек
            RequestBtn.Visibility = visibility; // Кнопка запросов
        }

        private void ClerkControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed; // Установка видимости
            RegCardBtn.Visibility = visibility; // Кнопка карточек
            DocumentBtn.Visibility = visibility;// Кнопка документов
            SimpleRepBtn.Visibility = visibility;// Кнопка простого отчета
        }

        private void ArchivariusControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed; // Установка видимости
            RequestBtn.Visibility = visibility; // Кнопка запросов
        }

        private void DocumentBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new DocumentPage()); // Переход на страницу документов
        }

        private void UserBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new UserPage()); // Переход на страницу пользователей
        }

        private void RegCardBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new RegCardPage()); // Переход на страницу карточек
        }

        private void RequestBtn_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new RequestPage()); // Переход на страницу запросов
        }

        private void SimpleRepBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog // Диалог сохранения файла Excel
            {
                FileName = "Простой отчет",     // Имя файла по умолчанию
                DefaultExt = ".xlsx",           // Расширение по умолчанию
                Filter = "Excel files (.xlsx)|*.xlsx" // Фильтр файлов
            };

            if (saveFileDialog.ShowDialog() == true) // Открытие диалога и проверка выбора
            {
                ExportExcel.ExportToExcel(saveFileDialog.FileName); // Экспорт данных в Excel
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog // Диалог сохранения файла Word
            {
                FileName = "Отчет",             // Имя файла по умолчанию
                DefaultExt = ".docx",           // Расширение по умолчанию
                Filter = "Word documents (.docx)|*.docx" // Фильтр файлов
            };

            if (saveFileDialog.ShowDialog() == true) // Открытие диалога и проверка выбора
            {
                ExportWord.ExportToWord(saveFileDialog.FileName); // Экспорт данных в Word
            }
        }
    }
}