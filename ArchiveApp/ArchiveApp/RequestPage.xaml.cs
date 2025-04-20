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
        private bool isAddingNewRow = false;        // Флаг добавления новой строки
        private Request newRequest;                 // Новый запрос для добавления
        private int currentUserId = UserData.CurrentUserId; // ID текущего пользователя
        public ObservableCollection<Request> Requests { get; set; } // Коллекция запросов
        public List<KeyValuePair<bool?, string>> StatusList { get; set; } // Список статусов
        public List<Document> Documents { get; set; } // Список документов
        public List<User> Users { get; set; }       // Список пользователей
        private List<Request> _allRequests;         // Поле для хранения полного списка запросов

        public RequestPage()
        {
            InitializeComponent();                  // Инициализация компонентов страницы
            DataContext = this;                     // Установка контекста данных
            Requests = new ObservableCollection<Request>(); // Инициализация коллекции запросов
            LoadStatusList();                      // Загрузка списка статусов
            LoadDocuments();                       // Загрузка списка документов
            LoadUsers();                           // Загрузка списка пользователей
            LoadData();                            // Загрузка данных запросов
            // Регистрируем обработчик события BeginningEdit
            DataGridTable.BeginningEdit += DataGridTable_BeginningEdit;
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>> // Создание списка статусов
            {
                new KeyValuePair<bool?, string>(true, "Принято"), // Статус "Принято"
                new KeyValuePair<bool?, string>(false, "Отклонено") // Статус "Отклонено"
            };
        }

        private void LoadDocuments()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                Documents = context.Document.ToList(); // Загрузка всех документов
            }
        }

        private void LoadUsers()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                Users = context.User.ToList();      // Загрузка всех пользователей
            }
        }

        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                _allRequests = context.Request      // Загрузка запросов с связанными данными
                    .Include(r => r.User)           // Включение данных пользователей
                    .Include(r => r.Document)       // Включение данных документов
                    .ToList();

                Requests.Clear();                   // Очистка текущей коллекции
                foreach (var req in _allRequests)   // Заполнение коллекции
                    Requests.Add(req);
            }
            DataGridTable.ItemsSource = Requests;   // Установка источника данных для DataGrid
            DataGridTable.IsReadOnly = true;        // Установка режима "только чтение"
        }

        private void DeleteSelectedRequests()
        {
            var selectedRequests = DataGridTable.SelectedItems.Cast<Request>().ToList(); // Получение выбранных запросов
            if (selectedRequests.Count == 0)        // Проверка наличия выбранных элементов
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {selectedRequests.Count} элементов?", // Подтверждение удаления
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                    {
                        foreach (var req in selectedRequests) // Удаление каждого запроса
                        {
                            var reqToRemove = context.Request.Find(req.Id); // Поиск запроса в базе
                            if (reqToRemove != null)
                                context.Request.Remove(reqToRemove); // Удаление запроса
                        }
                        context.SaveChanges();          // Сохранение изменений
                    }
                    MessageBox.Show("Данные удалены!"); // Уведомление об успешном удалении
                    LoadData();                        // Перезагрузка данных
                }
                catch (Exception ex)                    // Обработка ошибок
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRequests(); // Вызываем метод удаления
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

                // Проверяем, является ли текущий столбец последним (Документ)
                if (currentColumnIndex == totalColumns - 1)
                {
                    try
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        ToggleEditMode(); // Вызываем логику кнопки Edit (сохранение)
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка валидации данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                // Обычная обработка Enter для перехода между ячейками
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
                // Отменяем редактирование для всех строк, кроме новой
                e.Cancel = true;
            }
        }

        private void ReqSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ReqSearchBox.Text.ToLower(); // Получение текста поиска (в нижнем регистре)

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поле поиска пустое, показываем все запросы
                Requests.Clear();
                foreach (var req in _allRequests)
                    Requests.Add(req);
            }
            else
            {
                // Фильтрация запросов по всем полям
                var filteredRequests = _allRequests
                    .Where(req =>
                        // Проверка всех полей (преобразование в строку и нижний регистр)
                        req.Request_Date.ToString("dd.MM.yyyy").ToLower().Contains(searchText) ||
                        (req.Reason?.ToLower().Contains(searchText) == true) ||
                        (req.Status.HasValue && (req.Status.Value ? "принято" : "отклонено").Contains(searchText)) ||
                        (req.User?.Name?.ToLower().Contains(searchText) == true) ||
                        (req.Document?.Title?.ToLower().Contains(searchText) == true)
                    )
                    .ToList();

                Requests.Clear();
                foreach (var req in filteredRequests)
                    Requests.Add(req);
            }
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            ReqSearchBox.Text = string.Empty; // Очистка поля поиска
            Requests.Clear();
            foreach (var req in _allRequests)
                Requests.Add(req);
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
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
                // Игнорируем клики по интерактивным элементам
                if (clickedElement is Button || clickedElement is TextBox ||
                    clickedElement is TextBlock || clickedElement is Image ||
                    clickedElement is DataGrid || clickedElement is ComboBox)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // Если клик был на пустом месте и ReqSearchBox в фокусе, снимаем фокус
            if (isEmptySpace && Keyboard.FocusedElement == ReqSearchBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Esc или Enter
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // Если ReqSearchBox в фокусе, снимаем фокус
                if (Keyboard.FocusedElement == ReqSearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
            }
        }
    }
}