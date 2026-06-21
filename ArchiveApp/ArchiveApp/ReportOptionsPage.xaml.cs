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
            Dispatcher.BeginInvoke(new Action(ApplyCheckboxLogicByFormat), System.Windows.Threading.DispatcherPriority.Loaded);

            System.Diagnostics.Debug.WriteLine($"ReportOptionsPage initialized with IsFullReport: {IsFullReport}");
        }

        private void LoadComboBoxData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                var documents = new List<object> { new { Id = -1, Title = "Все записи" } };
                documents.AddRange(context.Document.ToList().Select(d => new { Id = d.Id, Title = d.Title }));
                DocumentsComboBox.ItemsSource = documents;
                DocumentsComboBox.DisplayMemberPath = "Title";
                DocumentsComboBox.SelectedValuePath = "Id";
                DocumentsComboBox.SelectedValue = -1;

                var requests = new List<object> { new { Id = -1, Reason = "Все записи" } };
                requests.AddRange(context.Request.ToList().Select(r => new { Id = r.Id, Reason = r.Reason }));
                RequestsComboBox.ItemsSource = requests;
                RequestsComboBox.DisplayMemberPath = "Reason";
                RequestsComboBox.SelectedValuePath = "Id";
                RequestsComboBox.SelectedValue = -1;
                var users = new List<object> { new { Id = -1, Name = "Все записи" } };
                users.AddRange(context.User.ToList().Select(u => new { Id = u.Id, Name = u.Name }));
                UsersComboBox.ItemsSource = users;
                UsersComboBox.DisplayMemberPath = "Name";
                UsersComboBox.SelectedValuePath = "Id";
                UsersComboBox.SelectedValue = -1;
                var regCards = new List<object> { new { Id = -1, Title = "Все записи" } };
                regCards.AddRange(context.Registration_Card.ToList().Select(c => new { Id = c.Id, Title = c.Document?.Title }));
                RegCardsComboBox.ItemsSource = regCards;
                RegCardsComboBox.DisplayMemberPath = "Title";
                RegCardsComboBox.SelectedValuePath = "Id";
                RegCardsComboBox.SelectedValue = -1;
            }
        }

        /// <summary>
        /// Настраивает видимость и состояние чекбоксов в зависимости от роли пользователя.
        /// Администратор видит все таблицы, архивариус — кроме пользователей,
        /// делопроизводитель — только документы и регистрационные карты.
        /// </summary>
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
            UpdateSingleSelectionLock();
        }

        private void AllTablesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DocumentsCheckBox == null) return;

            DocumentsCheckBox.IsChecked = false;
            RequestsCheckBox.IsChecked = false;
            UsersCheckBox.IsChecked = false;
            RegCardsCheckBox.IsChecked = false;

            UpdateIndividualCheckBoxes();
            UpdateSingleSelectionLock();
        }

        private void TableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
            UpdateSingleSelectionLock();
        }

        private void TableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateIndividualCheckBoxes();
            UpdateSingleSelectionLock();
        }

        private void FormatRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AllTablesCheckBox == null) return;

            bool isExcel = ExcelRadio?.IsChecked == true;
            bool isWordOrPdf = (WordRadio?.IsChecked == true) || (PdfRadio?.IsChecked == true);

            IsWordOrPdf = isWordOrPdf;

            if (isExcel)
            {
                IsTableFormat = true;
                if (TableFormatRadio != null) TableFormatRadio.IsChecked = true;
                if (TextFormatRadio != null)
                {
                    TextFormatRadio.IsChecked = false;
                    TextFormatRadio.IsEnabled = false;
                }
                AllTablesCheckBox.IsChecked = true;
            }
            else
            {
                if (TextFormatRadio != null)
                    TextFormatRadio.IsEnabled = true;

                IsTableFormat = TableFormatRadio?.IsChecked == true;
            }
            ApplyCheckboxLogicByFormat();
            Dispatcher.BeginInvoke(new Action(ApplyCheckboxLogicByFormat),
                System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// Применяет логику видимости элементов в зависимости от формата вывода.
        /// Для текстового формата скрывает все таблицы, кроме Документов,
        /// и убирает выбор "Все записи" в пользу выбора конкретной записи.
        /// </summary>
        private void ApplyCheckboxLogicByFormat()
        {
            if (AllTablesCheckBox == null) return;

            bool isTextFormat = !IsTableFormat &&
                               (WordRadio?.IsChecked == true || PdfRadio?.IsChecked == true);

            if (isTextFormat)
            {
                AllTablesCheckBox.Visibility = Visibility.Collapsed;
                AllTablesCheckBox.IsChecked = false;

                DocumentsCheckBox.Visibility = Visibility.Visible;
                DocumentsCheckBox.IsChecked = true;
                DocumentsCheckBox.IsEnabled = true;

                RequestsCheckBox.Visibility = Visibility.Collapsed;
                UsersCheckBox.Visibility = Visibility.Collapsed;
                RegCardsCheckBox.Visibility = Visibility.Collapsed;

                DocumentsComboBox.Visibility = Visibility.Visible;
                RequestsComboBox.Visibility = Visibility.Collapsed;
                UsersComboBox.Visibility = Visibility.Collapsed;
                RegCardsComboBox.Visibility = Visibility.Collapsed;

                UpdateDocumentsComboBoxForTextFormat();
            }
            else
            {
                AllTablesCheckBox.Visibility = Visibility.Visible;
                AllTablesCheckBox.IsEnabled = true;

                DocumentsCheckBox.Visibility = Visibility.Visible;
                RequestsCheckBox.Visibility = Visibility.Visible;
                UsersCheckBox.Visibility = _userRole == "Администратор" ? Visibility.Visible : Visibility.Collapsed;
                RegCardsCheckBox.Visibility = Visibility.Visible;

                DocumentsCheckBox.IsEnabled = true;
                RequestsCheckBox.IsEnabled = true;
                UsersCheckBox.IsEnabled = _userRole == "Администратор";
                RegCardsCheckBox.IsEnabled = true;

                UpdateDocumentsComboBoxForTextFormat();

                UpdateIndividualCheckBoxes();
                UpdateSingleSelectionLock();
            }
        }

        private void UpdateDocumentsComboBoxForTextFormat()
        {
            if (DocumentsComboBox != null && DocumentsComboBox.ItemsSource != null)
            {
                var currentSelection = DocumentsComboBox.SelectedValue;

                var currentSource = DocumentsComboBox.ItemsSource;
                var list = new List<object>();

                bool isTextFormat = !IsTableFormat && (WordRadio?.IsChecked == true || PdfRadio?.IsChecked == true);

                if (isTextFormat)
                {
                    foreach (var item in currentSource)
                    {
                        var prop = item.GetType().GetProperty("Id");
                        if (prop != null)
                        {
                            int id = Convert.ToInt32(prop.GetValue(item));
                            if (id != -1)
                            {
                                list.Add(item);
                            }
                        }
                    }
                    DocumentsComboBox.ItemsSource = list;

                    if (currentSelection != null && Convert.ToInt32(currentSelection) == -1)
                    {
                        if (list.Any())
                        {
                            DocumentsComboBox.SelectedItem = list.First();
                        }
                    }
                }
                else
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        var fullList = new List<object> { new { Id = -1, Title = "Все записи" } };
                        fullList.AddRange(context.Document.ToList().Select(d => new { Id = d.Id, Title = d.Title }));
                        DocumentsComboBox.ItemsSource = fullList;
                        if (currentSelection != null)
                        {
                            DocumentsComboBox.SelectedValue = currentSelection;
                        }
                        else
                        {
                            DocumentsComboBox.SelectedValue = -1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Блокирует остальные чекбоксы, если выбран только один.
        /// Это предотвращает множественный выбор в режиме просмотра конкретной записи.
        /// </summary>
        private void UpdateSingleSelectionLock()
        {
            if (AllTablesCheckBox.IsChecked == true) return;

            int checkedCount = 0;
            if (DocumentsCheckBox.IsChecked == true) checkedCount++;
            if (RequestsCheckBox.IsChecked == true) checkedCount++;
            if (UsersCheckBox.IsChecked == true) checkedCount++;
            if (RegCardsCheckBox.IsChecked == true) checkedCount++;

            bool lockOthers = checkedCount == 1;
            DocumentsCheckBox.IsEnabled = !lockOthers || DocumentsCheckBox.IsChecked == true;
            RequestsCheckBox.IsEnabled = !lockOthers || RequestsCheckBox.IsChecked == true;
            UsersCheckBox.IsEnabled = (!lockOthers || UsersCheckBox.IsChecked == true) && _userRole == "Администратор";
            RegCardsCheckBox.IsEnabled = !lockOthers || RegCardsCheckBox.IsChecked == true;
        }

        private void DocumentsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsComboBox.SelectedValue != null)
            {
                int selectedId = Convert.ToInt32(DocumentsComboBox.SelectedValue);
                if (selectedId == -1)
                {
                    if (SelectedRecordIds.ContainsKey("Documents"))
                        SelectedRecordIds.Remove("Documents");
                }
                else
                {
                    SelectedRecordIds["Documents"] = new List<int> { selectedId };
                }
            }
        }

        private void RequestsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsComboBox.SelectedValue != null)
            {
                int selectedId = Convert.ToInt32(RequestsComboBox.SelectedValue);
                if (selectedId == -1)
                {
                    if (SelectedRecordIds.ContainsKey("Requests"))
                        SelectedRecordIds.Remove("Requests");
                }
                else
                {
                    SelectedRecordIds["Requests"] = new List<int> { selectedId };
                }
            }
        }

        private void UsersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsersComboBox.SelectedValue != null)
            {
                int selectedId = Convert.ToInt32(UsersComboBox.SelectedValue);
                if (selectedId == -1)
                {
                    if (SelectedRecordIds.ContainsKey("Users"))
                        SelectedRecordIds.Remove("Users");
                }
                else
                {
                    SelectedRecordIds["Users"] = new List<int> { selectedId };
                }
            }
        }

        private void RegCardsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RegCardsComboBox.SelectedValue != null)
            {
                int selectedId = Convert.ToInt32(RegCardsComboBox.SelectedValue);
                if (selectedId == -1)
                {
                    if (SelectedRecordIds.ContainsKey("RegistrationCards"))
                        SelectedRecordIds.Remove("RegistrationCards");
                }
                else
                {
                    SelectedRecordIds["RegistrationCards"] = new List<int> { selectedId };
                }
            }
        }

        /// <summary>
        /// Основной метод создания отчета.
        /// Собирает выбранные параметры, проверяет наличие данных за указанный период,
        /// формирует детали для аудита и вызывает событие ReportOptionsSelected.
        /// </summary>
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
                ApplyCheckboxLogicByFormat();

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

                if (!isAllTablesSelected && !IsTableFormat)
                {
                    SelectedTables.Clear();
                    SelectedTables.Add("Documents");
                }
                if (!isAllTablesSelected)
                {
                    foreach (var table in SelectedTables)
                    {
                        bool isAllRecordsSelected = false;
                        if (table == "Documents" && DocumentsComboBox.SelectedValue != null && Convert.ToInt32(DocumentsComboBox.SelectedValue) == -1)
                            isAllRecordsSelected = true;
                        else if (table == "Requests" && RequestsComboBox.SelectedValue != null && Convert.ToInt32(RequestsComboBox.SelectedValue) == -1)
                            isAllRecordsSelected = true;
                        else if (table == "Users" && UsersComboBox.SelectedValue != null && Convert.ToInt32(UsersComboBox.SelectedValue) == -1)
                            isAllRecordsSelected = true;
                        else if (table == "RegistrationCards" && RegCardsComboBox.SelectedValue != null && Convert.ToInt32(RegCardsComboBox.SelectedValue) == -1)
                            isAllRecordsSelected = true;

                        if (!isAllRecordsSelected && SelectedRecordIds.ContainsKey(table) && !SelectedRecordIds[table].Any())
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
                    bool hasData = CheckDataExists(StartDate, EndDate, SelectedTables, SelectedRecordIds);
                    if (!hasData)
                    {
                        MessageBox.Show("За выбранный период нет данных для формирования отчета!",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

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

                string auditDetails = GenerateAuditDetails();
                AuditService.Log("Формирование отчета", "Report", auditDetails);

                ReportOptionsSelected?.Invoke(SelectedFormat, IsTableFormat, SelectedTables, SelectedRecordIds, StartDate, EndDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет наличие данных в выбранных таблицах за указанный период.
        /// Используется для предотвращения создания пустых отчетов.
        /// </summary>
        private bool CheckDataExists(DateTime? startDate, DateTime? endDate, List<string> selectedTables, Dictionary<string, List<int>> selectedRecordIds)
        {
            using (var context = new ArchiveBaseEntities())
            {
                List<int> docIds = new List<int>();
                List<int> reqIds = new List<int>();
                List<int> userIds = new List<int>();
                List<int> cardIds = new List<int>();

                if (selectedRecordIds != null)
                {
                    if (selectedRecordIds.ContainsKey("Documents") && selectedRecordIds["Documents"].Any())
                        docIds = selectedRecordIds["Documents"];
                    if (selectedRecordIds.ContainsKey("Requests") && selectedRecordIds["Requests"].Any())
                        reqIds = selectedRecordIds["Requests"];
                    if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"].Any())
                        userIds = selectedRecordIds["Users"];
                    if (selectedRecordIds.ContainsKey("RegistrationCards") && selectedRecordIds["RegistrationCards"].Any())
                        cardIds = selectedRecordIds["RegistrationCards"];
                }

                bool hasData = false;

                foreach (var table in selectedTables)
                {
                    switch (table)
                    {
                        case "Documents":
                            var docsQuery = context.Document.AsQueryable();
                            if (startDate.HasValue && endDate.HasValue)
                                docsQuery = docsQuery.Where(d => d.Receipt_Date >= startDate && d.Receipt_Date <= endDate);
                            if (docIds.Any())
                                docsQuery = docsQuery.Where(d => docIds.Contains(d.Id));
                            if (docsQuery.Any()) hasData = true;
                            break;
                        case "Requests":
                            var reqQuery = context.Request.AsQueryable();
                            if (startDate.HasValue && endDate.HasValue)
                                reqQuery = reqQuery.Where(r => r.Request_Date >= startDate && r.Request_Date <= endDate);
                            if (reqIds.Any())
                                reqQuery = reqQuery.Where(r => reqIds.Contains(r.Id));
                            if (reqQuery.Any()) hasData = true;
                            break;
                        case "Users":
                            var userQuery = context.User.AsQueryable();
                            if (userIds.Any())
                                userQuery = userQuery.Where(u => userIds.Contains(u.Id));
                            if (userQuery.Any()) hasData = true;
                            break;
                        case "RegistrationCards":
                            var cardQuery = context.Registration_Card.AsQueryable();
                            if (startDate.HasValue && endDate.HasValue)
                                cardQuery = cardQuery.Where(c => c.Registration_Date >= startDate && c.Registration_Date <= endDate);
                            if (cardIds.Any())
                                cardQuery = cardQuery.Where(c => cardIds.Contains(c.Id));
                            if (cardQuery.Any()) hasData = true;
                            break;
                    }
                }
                return hasData;
            }
        }

        private string GenerateAuditDetails()
        {
            string formatInfo = $"Формат: {SelectedFormat}, Представление: {(IsTableFormat ? "Таблица" : "Текст")}";

            string tablesInfo;
            bool isAllTables = AllTablesCheckBox.IsChecked == true;
            if (isAllTables)
            {
                tablesInfo = "Все таблицы";
            }
            else
            {
                tablesInfo = "Таблицы: " + string.Join(", ", SelectedTables);
            }

            string periodInfo = "";
            if (IsFullReport && StartDate.HasValue && EndDate.HasValue)
            {
                periodInfo = $", Период: с {StartDate.Value:dd.MM.yyyy} по {EndDate.Value:dd.MM.yyyy}";
            }
            else if (!IsFullReport)
            {
                periodInfo = ", Простой отчет (без периода)";
            }

            return $"{formatInfo}. {tablesInfo}{periodInfo}";
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

        private void TableFormatRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AllTablesCheckBox == null) return;

            IsTableFormat = true;
            IsWordOrPdf = (WordRadio?.IsChecked == true) || (PdfRadio?.IsChecked == true);
            AllTablesCheckBox.Visibility = Visibility.Visible;
            AllTablesCheckBox.IsEnabled = true;
            if (_userRole == "Администратор")
            {
                AllTablesCheckBox.IsChecked = true;
            }
            else if (_userRole == "Архивариус")
            {
                AllTablesCheckBox.IsChecked = true;
            }
            else if (_userRole == "Делопроизводитель")
            {
                DocumentsCheckBox.IsChecked = true;
                RegCardsCheckBox.IsChecked = true;
                AllTablesCheckBox.IsChecked = false;
            }

            ApplyCheckboxLogicByFormat();
            Dispatcher.BeginInvoke(new Action(ApplyCheckboxLogicByFormat),
                System.Windows.Threading.DispatcherPriority.Render);
        }

        private void TextFormatRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AllTablesCheckBox == null) return;

            IsTableFormat = false;
            IsWordOrPdf = (WordRadio?.IsChecked == true) || (PdfRadio?.IsChecked == true);
            ApplyCheckboxLogicByFormat();
            Dispatcher.BeginInvoke(new Action(ApplyCheckboxLogicByFormat),
                System.Windows.Threading.DispatcherPriority.Render);
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

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}