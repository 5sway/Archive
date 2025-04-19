using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class ReportOptionsPage : Page
    {
        public bool IsFullReport { get; set; }
        public string SelectedFormat { get; private set; }
        public List<string> SelectedTables { get; private set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        private readonly string _userRole;

        public event Action<string, List<string>, DateTime?, DateTime?> ReportOptionsSelected;

        public ReportOptionsPage(bool isFullReport, string userRole)
        {
            InitializeComponent();
            IsFullReport = isFullReport;
            _userRole = userRole;
            SelectedTables = new List<string>();
            SelectedFormat = "Word";
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            DataContext = this;

            SetupCheckBoxesByRole();
        }

        private void SetupCheckBoxesByRole()
        {
            try
            {
                // Изначально отключаем все чекбоксы
                DocumentsCheckBox.Visibility = Visibility.Collapsed;
                RequestsCheckBox.Visibility = Visibility.Collapsed;
                UsersCheckBox.Visibility = Visibility.Collapsed;
                RegCardsCheckBox.Visibility = Visibility.Collapsed;
                AllTablesCheckBox.IsChecked = false;

                switch (_userRole)
                {
                    case "Администратор":
                        DocumentsCheckBox.Visibility = Visibility.Visible;
                        RequestsCheckBox.Visibility = Visibility.Visible;
                        UsersCheckBox.Visibility = Visibility.Visible;
                        RegCardsCheckBox.Visibility = Visibility.Visible;
                        AllTablesCheckBox.Visibility = Visibility.Visible;
                        AllTablesCheckBox.IsChecked = true;
                        break;

                    case "Архивариус":
                        DocumentsCheckBox.Visibility = Visibility.Visible;
                        RequestsCheckBox.Visibility = Visibility.Visible;
                        RegCardsCheckBox.Visibility = Visibility.Visible;
                        AllTablesCheckBox.Visibility = Visibility.Visible;
                        AllTablesCheckBox.IsChecked = true;
                        break;

                    case "Делопроизводитель":
                        DocumentsCheckBox.Visibility = Visibility.Visible;
                        RegCardsCheckBox.Visibility = Visibility.Visible;
                        AllTablesCheckBox.Visibility = Visibility.Collapsed;
                        DocumentsCheckBox.IsChecked = true;
                        RegCardsCheckBox.IsChecked = true;
                        DocumentsCheckBox.IsEnabled = true;
                        RegCardsCheckBox.IsEnabled = true;
                        break;
                }

                UpdateIndividualCheckBoxes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке чекбоксов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateIndividualCheckBoxes()
        {
            if (AllTablesCheckBox.IsChecked == true)
            {
                DocumentsCheckBox.IsChecked = true;
                RequestsCheckBox.IsChecked = true;
                if (_userRole == "Администратор") UsersCheckBox.IsChecked = true;
                RegCardsCheckBox.IsChecked = true;

                DocumentsCheckBox.IsEnabled = false;
                RequestsCheckBox.IsEnabled = false;
                UsersCheckBox.IsEnabled = false;
                RegCardsCheckBox.IsEnabled = false;
            }
            else
            {
                DocumentsCheckBox.IsEnabled = true;
                RequestsCheckBox.IsEnabled = true;
                UsersCheckBox.IsEnabled = _userRole == "Администратор";
                RegCardsCheckBox.IsEnabled = true;
            }
        }

        private void AllTablesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        private void AllTablesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DocumentsCheckBox.IsChecked = false;
            RequestsCheckBox.IsChecked = false;
            UsersCheckBox.IsChecked = false;
            RegCardsCheckBox.IsChecked = false;

            DocumentsCheckBox.IsEnabled = true;
            RequestsCheckBox.IsEnabled = true;
            UsersCheckBox.IsEnabled = _userRole == "Администратор";
            RegCardsCheckBox.IsEnabled = true;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Определение формата
                if (WordRadio.IsChecked == true)
                    SelectedFormat = "Word";
                else if (ExcelRadio.IsChecked == true)
                    SelectedFormat = "Excel";
                else if (PdfRadio.IsChecked == true)
                    SelectedFormat = "PDF";
                else
                {
                    MessageBox.Show("Выберите формат отчета!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Определение выбранных таблиц
                SelectedTables.Clear();
                if (DocumentsCheckBox.IsChecked == true)
                    SelectedTables.Add("Documents");
                if (RequestsCheckBox.IsChecked == true)
                    SelectedTables.Add("Requests");
                if (UsersCheckBox.IsChecked == true)
                    SelectedTables.Add("Users");
                if (RegCardsCheckBox.IsChecked == true)
                    SelectedTables.Add("RegistrationCards");

                if (!SelectedTables.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну таблицу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsFullReport)
                {
                    if (!StartDate.HasValue || !EndDate.HasValue)
                    {
                        MessageBox.Show("Выберите даты периода!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (StartDate > EndDate)
                    {
                        MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    StartDate = null;
                    EndDate = null;
                }

                ReportOptionsSelected?.Invoke(SelectedFormat, SelectedTables, StartDate, EndDate);
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
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
                // Игнорируем клики по интерактивным элементам, включая DatePicker
                if (clickedElement is Button || clickedElement is TextBox ||
                    clickedElement is TextBlock || clickedElement is Image ||
                    clickedElement is DataGrid || clickedElement is ComboBox ||
                    clickedElement is CheckBox || clickedElement is RadioButton ||
                    clickedElement is DatePicker)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // Если клик был на пустом месте и один из DatePicker в фокусе, снимаем фокус
            if (isEmptySpace && (Keyboard.FocusedElement == StartDatePicker || Keyboard.FocusedElement == EndDatePicker))
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Esc или Enter
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // Если один из DatePicker в фокусе, снимаем фокус
                if (Keyboard.FocusedElement == StartDatePicker || Keyboard.FocusedElement == EndDatePicker)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
            }
        }
    }
}