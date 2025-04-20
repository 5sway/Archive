using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ArchiveApp
{
    public partial class MainMenuPage : Page
    {
        private string _Role;
        public event Action OnRoleChanged;

        public MainMenuPage(string role)
        {
            InitializeComponent();
            _Role = role ?? throw new ArgumentNullException(nameof(role), "Роль пользователя не может быть null");
            SetPermissionsBasedOnRole();
            OnRoleChanged?.Invoke();
        }

        private void SetPermissionsBasedOnRole()
        {
            var mainGrid = this.Content as Grid;
            switch (_Role)
            {
                case "Администратор":
                    AdminControlsVisibility(true);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    Grid.SetRow(RegCardBtn, 2);
                    Grid.SetColumn(RegCardBtn, 1);
                    DocumentBtn.BorderThickness = new Thickness(1, 1, 1, 1);
                    SimpleRepBtn.BorderThickness = new Thickness(0, 1, 1, 1);
                    RegCardBtn.BorderThickness = new Thickness(1, 0, 1, 1);
                    Grid.SetRow(RequestBtn, 2);
                    Grid.SetColumn(RequestBtn, 2);
                    RequestBtn.BorderThickness = new Thickness(0, 0, 1, 1);
                    break;
                case "Делопроизводитель":
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(false);
                    Grid.SetRow(RegCardBtn, 1);
                    Grid.SetColumn(RegCardBtn, 3);
                    RegCardBtn.BorderThickness = new Thickness(0, 1, 1, 1);
                    break;
                case "Архивариус":
                    AdminControlsVisibility(false);
                    ClerkControlsVisibility(true);
                    ArchivariusControlsVisibility(true);
                    Grid.SetRow(RegCardBtn, 1);
                    Grid.SetColumn(RegCardBtn, 3);
                    RegCardBtn.BorderThickness = new Thickness(0, 1, 1, 1);
                    RequestBtn.BorderThickness = new Thickness(1, 0, 1, 1);
                    break;
                default:
                    MessageBox.Show($"Неизвестная роль: {_Role}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void AdminControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            ReportBtn.Visibility = visibility;
            UserBtn.Visibility = visibility;
            RegCardBtn.Visibility = visibility;
            RequestBtn.Visibility = visibility;
        }

        private void ClerkControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            RegCardBtn.Visibility = visibility;
            DocumentBtn.Visibility = visibility;
            SimpleRepBtn.Visibility = visibility;
        }

        private void ArchivariusControlsVisibility(bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            RequestBtn.Visibility = visibility;
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

        private void SimpleRepBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var optionsPage = new ReportOptionsPage(false, _Role);
                optionsPage.ReportOptionsSelected += (format, tables, startDate, endDate) =>
                {
                    HandleReportOptions(format, tables, startDate, endDate, "Простой отчет");
                };
                Manager.MainFrame.Navigate(optionsPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в SimpleRepBtn_Click: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var optionsPage = new ReportOptionsPage(true, _Role);
                optionsPage.ReportOptionsSelected += (format, tables, startDate, endDate) =>
                {
                    HandleReportOptions(format, tables, startDate, endDate, "Отчет");
                };
                Manager.MainFrame.Navigate(optionsPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в ReportBtn_Click: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleReportOptions(string format, List<string> tables, DateTime? startDate, DateTime? endDate, string defaultFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(format) || tables == null)
                {
                    MessageBox.Show("Ошибка: параметры отчета не выбраны!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    FileName = defaultFileName,
                    DefaultExt = GetDefaultExtension(format),
                    Filter = GetFileFilter(format)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    // Убедимся, что файл имеет правильное расширение
                    string correctExtension = GetDefaultExtension(format);
                    if (!filePath.EndsWith(correctExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = Path.ChangeExtension(filePath, correctExtension);
                    }

                    ExportReport(filePath, format, tables, _Role, startDate, endDate);
                    // Navigate to MainMenuPage only if the file was saved
                    Manager.MainFrame.Navigate(new MainMenuPage(_Role));
                }
                // If the user cancels the SaveFileDialog, stay on ReportOptionsPage (no navigation)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в HandleReportOptions: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetDefaultExtension(string format)
        {
            switch (format.ToLower())
            {
                case "word":
                    return ".docx";
                case "excel":
                    return ".xlsx";
                case "pdf":
                    return ".pdf";
                default:
                    return ".docx";
            }
        }

        private string GetFileFilter(string format)
        {
            switch (format.ToLower())
            {
                case "word":
                    return "Word documents (*.docx)|*.docx";
                case "excel":
                    return "Excel files (*.xlsx)|*.xlsx";
                case "pdf":
                    return "PDF files (*.pdf)|*.pdf";
                default:
                    return "Word documents (*.docx)|*.docx";
            }
        }

        private void ExportReport(string filePath, string format, List<string> tables, string role, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(format) || tables == null || string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("Ошибка: некорректные параметры для экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                switch (format.ToLower())
                {
                    case "word":
                    case "pdf":
                        ExportWord.ExportToWord(filePath, tables, startDate, endDate, role, format);
                        break;
                    case "excel":
                        ExportExcel.ExportToExcel(filePath, tables, role, startDate, endDate);
                        break;
                    default:
                        MessageBox.Show($"Неподдерживаемый формат: {format}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в ExportReport: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}