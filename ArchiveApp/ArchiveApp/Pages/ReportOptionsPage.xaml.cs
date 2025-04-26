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
        public bool IsWordOrPdf { get; private set; }
        public string SelectedFormat { get; private set; }
        public bool IsTableFormat { get; private set; }
        public List<string> SelectedTables { get; private set; }
        public Dictionary<string, List<int>> SelectedRecordIds { get; private set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        private readonly string _userRole;

        public event Action<string, bool, List<string>, Dictionary<string, List<int>>, DateTime?, DateTime?> ReportOptionsSelected;

        public ReportOptionsPage(bool isFullReport, string userRole)
        {
            InitializeComponent();
            IsFullReport = isFullReport;
            _userRole = userRole;
            SelectedTables = new List<string>();
            SelectedRecordIds = new Dictionary<string, List<int>>();
            SelectedFormat = "Word";
            IsTableFormat = true;
            IsWordOrPdf = true;
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            DataContext = this;

            SetupCheckBoxesByRole();
            LoadComboBoxData();

            // Отладка: проверяем значение IsFullReport
            System.Diagnostics.Debug.WriteLine($"ReportOptionsPage initialized with IsFullReport: {IsFullReport}");
        }

        private void LoadComboBoxData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                DocumentsComboBox.ItemsSource = context.Document.ToList().Select(d => new { Id = d.Id, Title = d.Title }).ToList();
                DocumentsComboBox.DisplayMemberPath = "Title";
                DocumentsComboBox.SelectedValuePath = "Id";

                RequestsComboBox.ItemsSource = context.Request.ToList().Select(r => new { Id = r.Id, Reason = r.Reason }).ToList();
                RequestsComboBox.DisplayMemberPath = "Reason";
                RequestsComboBox.SelectedValuePath = "Id";

                UsersComboBox.ItemsSource = context.User.ToList().Select(u => new { Id = u.Id, Name = u.Name }).ToList();
                UsersComboBox.DisplayMemberPath = "Name";
                UsersComboBox.SelectedValuePath = "Id";

                RegCardsComboBox.ItemsSource = context.Registration_Card.ToList().Select(c => new { Id = c.Id, Title = c.Document?.Title }).ToList();
                RegCardsComboBox.DisplayMemberPath = "Title";
                RegCardsComboBox.SelectedValuePath = "Id";
            }
        }

        private void SetupCheckBoxesByRole()
        {
            try
            {
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

                DocumentsComboBox.Visibility = Visibility.Collapsed;
                RequestsComboBox.Visibility = Visibility.Collapsed;
                UsersComboBox.Visibility = Visibility.Collapsed;
                RegCardsComboBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                DocumentsCheckBox.IsEnabled = true;
                RequestsCheckBox.IsEnabled = true;
                UsersCheckBox.IsEnabled = _userRole == "Администратор";
                RegCardsCheckBox.IsEnabled = true;

                DocumentsComboBox.Visibility = DocumentsCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                RequestsComboBox.Visibility = RequestsCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                UsersComboBox.Visibility = UsersCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                RegCardsComboBox.Visibility = RegCardsCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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

            DocumentsComboBox.Visibility = Visibility.Collapsed;
            RequestsComboBox.Visibility = Visibility.Collapsed;
            UsersComboBox.Visibility = Visibility.Collapsed;
            RegCardsComboBox.Visibility = Visibility.Collapsed;
        }

        private void TableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        private void TableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
        }

        private void FormatRadio_Checked(object sender, RoutedEventArgs e)
        {
            IsWordOrPdf = WordRadio.IsChecked == true || PdfRadio.IsChecked == true;
            // Уведомляем привязки об изменении IsWordOrPdf
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsWordOrPdf)));
            }
            // Отладка: проверяем состояние
            System.Diagnostics.Debug.WriteLine($"FormatRadio_Checked: IsFullReport={IsFullReport}, IsWordOrPdf={IsWordOrPdf}");
        }

        private void DocumentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsComboBox.SelectedValue != null)
            {
                if (!SelectedRecordIds.ContainsKey("Documents"))
                    SelectedRecordIds["Documents"] =

 new List<int>();
                SelectedRecordIds["Documents"].Add((int)DocumentsComboBox.SelectedValue);
            }
        }

        private void RequestsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsComboBox.SelectedValue != null)
            {
                if (!SelectedRecordIds.ContainsKey("Requests"))
                    SelectedRecordIds["Requests"] = new List<int>();
                SelectedRecordIds["Requests"].Add((int)RequestsComboBox.SelectedValue);
            }
        }

        private void UsersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersComboBox.SelectedValue != null)
            {
                if (!SelectedRecordIds.ContainsKey("Users"))
                    SelectedRecordIds["Users"] = new List<int>();
                SelectedRecordIds["Users"].Add((int)UsersComboBox.SelectedValue);
            }
        }

        private void RegCardsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RegCardsComboBox.SelectedValue != null)
            {
                if (!SelectedRecordIds.ContainsKey("RegistrationCards"))
                    SelectedRecordIds["RegistrationCards"] = new List<int>();
                SelectedRecordIds["RegistrationCards"].Add((int)RegCardsComboBox.SelectedValue);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                IsTableFormat = TableFormatRadio.IsChecked == true;

                SelectedTables.Clear();
                if (DocumentsCheckBox.IsChecked == true) SelectedTables.Add("Documents");
                if (RequestsCheckBox.IsChecked == true) SelectedTables.Add("Requests");
                if (UsersCheckBox.IsChecked == true) SelectedTables.Add("Users");
                if (RegCardsCheckBox.IsChecked == true) SelectedTables.Add("RegistrationCards");

                if (!SelectedTables.Any())
                {
                    MessageBox.Show("Выберите хотя бы одну таблицу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool isAllTablesSelected = AllTablesCheckBox != null && AllTablesCheckBox.IsChecked == true;
                if (!isAllTablesSelected)
                {
                    foreach (var table in SelectedTables)
                    {
                        if (!SelectedRecordIds.ContainsKey(table) || !SelectedRecordIds[table].Any())
                        {
                            var tableNames = new Dictionary<string, string>
                            {
                                { "Documents", "Документы" },
                                { "Requests", "Запросы" },
                                { "Users", "Пользователи" },
                                { "RegistrationCards", "Регистрационные карты" }
                            };

                            string tableName = tableNames.ContainsKey(table) ? tableNames[table] : table;

                            MessageBox.Show($"Выберите хотя бы одну запись для таблицы '{tableName}'!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
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

                ReportOptionsSelected?.Invoke(SelectedFormat, IsTableFormat, SelectedTables, SelectedRecordIds, StartDate, EndDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;
            bool isEmptySpace = false;
            while (clickedElement != null)
            {
                if (clickedElement is Grid grid && grid.Name == "MainGrid")
                {
                    isEmptySpace = true;
                    break;
                }
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

            if (isEmptySpace && (Keyboard.FocusedElement == StartDatePicker || Keyboard.FocusedElement == EndDatePicker))
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement == StartDatePicker || Keyboard.FocusedElement == EndDatePicker)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }

        // Реализация INotifyPropertyChanged для уведомления о смене свойств
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}