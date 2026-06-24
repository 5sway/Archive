using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class RequestPage : Page
    {
        private bool isAddingNewRow = false;
        private Request newRequest;
        private int currentUserId = UserData.CurrentUserId;
        public ObservableCollection<Request> Requests { get; set; }
        public List<KeyValuePair<bool?, string>> StatusList { get; set; }
        public List<Document> Documents { get; set; }
        public List<User> Users { get; set; }
        private List<Request> _allRequests;

        public RequestPage()
        {
            InitializeComponent();
            DataContext = this;
            Requests = new ObservableCollection<Request>();
            LoadStatusList();
            LoadDocuments();
            LoadUsers();
            LoadData();
            DataGridTable.BeginningEdit += DataGridTable_BeginningEdit;
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>>
            {
                new KeyValuePair<bool?, string>(true, "Принято"),
                new KeyValuePair<bool?, string>(false, "Отклонено")
            };
        }

        private void LoadDocuments()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Documents = context.Document.ToList();
            }
        }

        private void LoadUsers()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Users = context.User.ToList();
            }
        }

        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                _allRequests = context.Request
                    .Include(r => r.User)
                    .Include(r => r.Document)
                    .ToList();

                Requests.Clear();
                foreach (var req in _allRequests)
                    Requests.Add(req);
            }
            DataGridTable.ItemsSource = Requests;
            DataGridTable.IsReadOnly = true;
        }

        private void DeleteSelectedRequests()
        {
            var selectedRequests = DataGridTable.SelectedItems.Cast<Request>().ToList();
            if (selectedRequests.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {selectedRequests.Count} элементов?",
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        foreach (var req in selectedRequests)
                        {
                            var reqToRemove = context.Request.Find(req.Id);
                            if (reqToRemove != null)
                                context.Request.Remove(reqToRemove);
                            AuditService.Log("Удалён запрос", "Request",
                            $"ID: {req.Id}, Причина: {req.Reason}");
                        }
                        context.SaveChanges();
                    }
                    MessageBox.Show("Данные удалены!");
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRequests();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode();
        }

        private void ToggleEditMode()
        {
            if (DataGridTable.IsReadOnly)
            {
                DataGridTable.IsReadOnly = false;
                EditBtn.Content = "Сохранить";
            }
            else
            {
                DataGridTable.IsReadOnly = true;
                EditBtn.Content = "Изменить";
                SaveChanges();
            }
        }

        /// <summary>
        /// Сохраняет изменения запросов, включая добавление новой записи.
        /// Проверяет заполнение обязательных полей (причина, документ).
        /// </summary>
        private void SaveChanges()
        {
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    if (isAddingNewRow && newRequest != null)
                    {
                        if (string.IsNullOrWhiteSpace(newRequest.Reason) || newRequest.Document_Id == 0)
                        {
                            RemoveEmptyRow();
                            MessageBox.Show("Обязательные поля не заполнены. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        context.Request.Add(new Request
                        {
                            Request_Date = newRequest.Request_Date,
                            Reason = newRequest.Reason,
                            Status = newRequest.Status,
                            User_Id = newRequest.User_Id,
                            Document_Id = newRequest.Document_Id
                        });
                        AuditService.Log("Создан запрос", "Request",
                        $"Причина: {newRequest.Reason}, Документ ID: {newRequest.Document_Id}");
                    }

                    foreach (var req in Requests.Where(r => r.Id != 0))
                    {
                        var reqToUpdate = context.Request.Find(req.Id);
                        if (reqToUpdate != null)
                        {
                            reqToUpdate.Request_Date = req.Request_Date;
                            reqToUpdate.Reason = req.Reason;
                            reqToUpdate.Status = req.Status;
                            reqToUpdate.Document_Id = req.Document_Id;
                        }
                    }
                    context.SaveChanges();
                }

                isAddingNewRow = false;
                newRequest = null;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveEmptyRow()
        {
            if (newRequest != null && Requests.Contains(newRequest))
                Requests.Remove(newRequest);
            isAddingNewRow = false;
            newRequest = null;
            DataGridTable.IsReadOnly = true;
            EditBtn.Content = "Изменить";
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNewRow) return;

            isAddingNewRow = true;
            var currentUser = Users.FirstOrDefault(u => u.Id == currentUserId);
            newRequest = new Request
            {
                Id = 0,
                Request_Date = DateTime.Now,
                Reason = "",
                Status = null,
                User_Id = currentUserId,
                Document_Id = 0,
                Document = null,
                User = currentUser
            };

            Requests.Add(newRequest);
            DataGridTable.SelectedItem = newRequest;
            DataGridTable.ScrollIntoView(newRequest);
            DataGridTable.IsReadOnly = false;
            EditBtn.Content = "Сохранить";
        }

        /// <summary>
        /// Обрабатывает нажатия клавиш в DataGrid запросов.
        /// Delete — удаление выбранных запросов.
        /// Enter — навигация по ячейкам или сохранение строки.
        /// </summary>
        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                e.Handled = true;
                DeleteSelectedRequests();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var currentCell = dataGrid.CurrentCell;
                if (currentCell.Column == null) return;

                int currentColumnIndex = currentCell.Column.DisplayIndex;
                int totalColumns = dataGrid.Columns.Count;

                if (currentColumnIndex == totalColumns - 1)
                {
                    try
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        ToggleEditMode();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка валидации данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                try
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                }
                catch (Exception)
                {
                    MessageBox.Show("Некорректные данные. Пожалуйста, исправьте значение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                int nextColumnIndex = currentColumnIndex + 1;
                int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item);

                if (nextColumnIndex < totalColumns)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex], dataGrid.Columns[nextColumnIndex]);
                }
                else if (currentRowIndex < dataGrid.Items.Count - 1)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex + 1], dataGrid.Columns[0]);
                }

                dataGrid.Dispatcher.InvokeAsync(() => dataGrid.BeginEdit(), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void DataGridTable_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (isAddingNewRow && e.Row.Item != newRequest)
            {
                e.Cancel = true;
            }
        }

        private void ReqSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ReqSearchBox.Text?.ToLower().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                Requests.Clear();
                foreach (var req in _allRequests)
                    Requests.Add(req);
                return;
            }

            // Поиск по индексированным полям
            var filteredRequests = _allRequests
                .Where(req =>
                    // Дата запроса (индекс IX_Request_Date)
                    req.Request_Date.ToString("dd.MM.yyyy").Contains(searchText) ||
                    // Статус (индекс IX_Request_Status)
                    (req.Status.HasValue && (req.Status.Value ? "принято" : "отклонено").Contains(searchText)) ||
                    // Причина
                    (req.Reason?.ToLower().Contains(searchText) == true) ||
                    // Имя пользователя
                    (req.User?.Name?.ToLower().Contains(searchText) == true) ||
                    (req.User?.Last_Name?.ToLower().Contains(searchText) == true) ||
                    // Название документа (индекс IX_Document_Title)
                    (req.Document?.Title?.ToLower().Contains(searchText) == true)
                )
                .ToList();

            Requests.Clear();
            foreach (var req in filteredRequests)
                Requests.Add(req);
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            ReqSearchBox.Text = string.Empty;
            Requests.Clear();
            foreach (var req in _allRequests)
                Requests.Add(req);
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
                    clickedElement is DataGrid || clickedElement is ComboBox)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            if (isEmptySpace && Keyboard.FocusedElement == ReqSearchBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement == ReqSearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }
    }
}