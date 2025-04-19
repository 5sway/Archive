using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class DocumentPage : Page
    {
        private bool isAddingNewRow = false;        // Флаг добавления новой строки
        private Document newDocument;               // Новый документ для добавления
        private List<string> _storageTypes;         // Список типов хранения
        private string currentUserRole = UserData.CurrentUserRole; // Роль текущего пользователя
        private List<Document> _allDocuments;       // Поле для хранения полного списка документов

        public List<string> StorageTypes            // Свойство для доступа к типам хранения
        {
            get { return _storageTypes; }
            set { _storageTypes = value; }
        }

        public DocumentPage()
        {
            InitializeComponent();                  // Инициализация компонентов страницы
            this.DataContext = this;                // Установка контекста данных
            LoadStorageTypes();                    // Загрузка типов хранения
            LoadData();                            // Загрузка данных документов
            if (currentUserRole == "Делопроизводитель") // Ограничение доступа для делопроизводителя
            {
                DelBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки удаления
                AddBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки добавления
                EditBtn.Visibility = Visibility.Collapsed; // Скрытие кнопки редактирования
                return;
            }
            // Регистрируем обработчик события BeginningEdit
            DataGridTable.BeginningEdit += DataGridTable_BeginningEdit;
        }

        private void LoadStorageTypes()
        {
            StorageTypes = new List<string>         // Инициализация списка типов хранения
            {
                "Бумажный",                        // Тип хранения "Бумажный"
                "Электронный"                      // Тип хранения "Электронный"
            };
        }

        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                _allDocuments = context.Document.ToList(); // Загрузка документов в DataGrid
                DataGridTable.ItemsSource = _allDocuments; // Установка источника данных
            }
            DataGridTable.IsReadOnly = true;        // Установка режима "только чтение"
        }

        private void DeleteSelectedDocuments()
        {
            var documentsForRemoving = DataGridTable.SelectedItems.Cast<Document>().ToList(); // Получение выбранных документов
            if (documentsForRemoving.Count == 0)    // Проверка наличия выбранных элементов
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {documentsForRemoving.Count} элементов?", // Подтверждение удаления
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                    {
                        foreach (var doc in documentsForRemoving) // Удаление каждого документа
                        {
                            var docToRemove = context.Document.Find(doc.Id); // Поиск документа в базе
                            if (docToRemove != null)
                            {
                                context.Registration_Card.RemoveRange(docToRemove.Registration_Card); // Удаление связанных карточек
                                context.Request.RemoveRange(docToRemove.Request); // Удаление связанных запросов
                                context.Document.Remove(docToRemove); // Удаление документа
                            }
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
            DeleteSelectedDocuments(); // Вызываем метод удаления
        }

        private void ToggleEditMode()
        {
            if (DataGridTable.IsReadOnly)           // Переключение в режим редактирования
            {
                DataGridTable.IsReadOnly = false;   // Разрешение редактирования
                EditBtn.Content = "Сохранить";      // Изменение текста кнопки
            }
            else                                    // Сохранение изменений
            {
                DataGridTable.IsReadOnly = true;    // Блокировка редактирования
                EditBtn.Content = "Изменить";       // Восстановление текста кнопки
                SaveChanges();                     // Сохранение изменений
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode(); // Вызываем метод переключения режима
        }

        private void SaveChanges()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                if (newDocument != null && isAddingNewRow) // Добавление нового документа
                {
                    // Проверка обязательных полей
                    if (string.IsNullOrWhiteSpace(newDocument.Title) ||
                        string.IsNullOrWhiteSpace(newDocument.Number) ||
                        string.IsNullOrWhiteSpace(newDocument.Source) ||
                        string.IsNullOrWhiteSpace(newDocument.Storage_Type))
                    {
                        RemoveEmptyRow();           // Удаление пустой строки при ошибке
                        MessageBox.Show("Обязательные поля не заполнены. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Проверка Copies_Count
                    if (newDocument.Copies_Count <= 0)
                    {
                        RemoveEmptyRow();
                        MessageBox.Show("Количество копий должно быть больше 0. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    context.Document.Add(newDocument); // Добавление нового документа в базу
                }

                foreach (var item in DataGridTable.Items) // Обновление существующих документов
                {
                    if (item is Document doc && doc != newDocument) // Проверка типа и исключение нового документа
                    {
                        var docToUpdate = context.Document.Find(doc.Id); // Поиск документа в базе
                        if (docToUpdate != null)    // Обновление полей документа
                        {
                            docToUpdate.Number = doc.Number;
                            docToUpdate.Receipt_Date = doc.Receipt_Date;
                            docToUpdate.Title = doc.Title;
                            docToUpdate.Annotation = doc.Annotation;
                            docToUpdate.Source = doc.Source;
                            docToUpdate.Copies_Count = doc.Copies_Count;
                            docToUpdate.Storage_Type = doc.Storage_Type;
                        }
                    }
                }
                context.SaveChanges();              // Сохранение изменений в базе
            }

            isAddingNewRow = false;                 // Сброс флага добавления
            newDocument = null;                     // Очистка нового документа
            LoadData();                            // Перезагрузка данных
        }

        private void RemoveEmptyRow()
        {
            var items = DataGridTable.ItemsSource as List<Document>; // Получение текущего списка документов
            if (items != null)                      // Удаление пустой строки
            {
                items.Remove(newDocument);          // Удаление нового документа из списка
                DataGridTable.ItemsSource = null;   // Сброс источника данных
                DataGridTable.ItemsSource = items;  // Переустановка источника данных
            }
            isAddingNewRow = false;                 // Сброс флага добавления
            newDocument = null;                     // Очистка нового документа
            DataGridTable.IsReadOnly = true;        // Установка режима "только чтение"
            EditBtn.Content = "Изменить";           // Восстановление текста кнопки
        }

        private void DataGridTable_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (isAddingNewRow && e.Row.Item == newDocument)
            {
                // Даём время на обновление данных перед проверкой
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var document = e.Row.Item as Document;
                    if (document != null)
                    {
                        // Проверка обязательных полей
                        if (string.IsNullOrWhiteSpace(document.Title) ||
                            string.IsNullOrWhiteSpace(document.Number) ||
                            string.IsNullOrWhiteSpace(document.Source) ||
                            string.IsNullOrWhiteSpace(document.Storage_Type))
                        {
                            RemoveEmptyRow();
                            MessageBox.Show("Обязательные поля не заполнены. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Проверка Copies_Count
                        if (document.Copies_Count <= 0)
                        {
                            RemoveEmptyRow();
                            MessageBox.Show("Количество копий должно быть больше 0. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && currentUserRole != "Делопроизводитель") // Обработка нажатия Delete
            {
                e.Handled = true; // Предотвращаем стандартное поведение
                DeleteSelectedDocuments(); // Вызываем метод удаления
            }
            else if (e.Key == Key.Enter) // Обработка нажатия Enter
            {
                e.Handled = true; // Отмена стандартного поведения
                DataGrid dataGrid = sender as DataGrid; // Получение DataGrid
                if (dataGrid == null) return;

                var currentCell = dataGrid.CurrentCell; // Текущая ячейка
                if (currentCell.Column == null) return;

                int currentColumnIndex = currentCell.Column.DisplayIndex; // Индекс текущей колонки
                int totalColumns = dataGrid.Columns.Count; // Общее количество столбцов

                // Проверяем, является ли текущий столбец последним (Storage_Type)
                if (currentColumnIndex == totalColumns - 1 && currentUserRole != "Делопроизводитель")
                {
                    try
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        ToggleEditMode(); // Вызываем логику кнопки Edit
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка валидации данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                // Попытка завершить редактирование текущей ячейки
                try
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {ex.Message}");
                    MessageBox.Show("Некорректный формат даты. Пожалуйста, исправьте значение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                int nextColumnIndex = currentColumnIndex + 1; // Следующий индекс колонки
                int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item); // Индекс текущей строки

                if (nextColumnIndex < totalColumns) // Переход к следующей колонке
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex], dataGrid.Columns[nextColumnIndex]);
                }
                else if (currentRowIndex < dataGrid.Items.Count - 1) // Переход к следующей строке
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex + 1], dataGrid.Columns[0]);
                }

                dataGrid.Dispatcher.InvokeAsync(() => dataGrid.BeginEdit(), System.Windows.Threading.DispatcherPriority.Input); // Запуск редактирования
            }
        }

        private void DataGridTable_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (isAddingNewRow && e.Row.Item != newDocument && currentUserRole != "Делопроизводитель")
            {
                // Отменяем редактирование для всех строк, кроме новой
                e.Cancel = true;
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNewRow) return;             // Защита от повторного добавления

            isAddingNewRow = true;                  // Установка флага добавления
            newDocument = new Document              // Создание нового документа
            {
                Receipt_Date = DateTime.Now,        // Текущая дата
                Number = "",                       // Пустой номер
                Title = "",                        // Пустое название
                Source = "",                       // Пустой источник
                Copies_Count = 0,                  // Количество копий по умолчанию
                Annotation = "",                   // Пустая аннотация
                Storage_Type = StorageTypes.FirstOrDefault() // Первый тип хранения
            };

            var items = DataGridTable.ItemsSource as List<Document>; // Получение текущего списка
            if (items != null)                      // Добавление нового документа в список
            {
                items.Add(newDocument);
                DataGridTable.ItemsSource = null;   // Сброс источника данных
                DataGridTable.ItemsSource = items;  // Переустановка источника данных
            }

            DataGridTable.SelectedItem = newDocument; // Установка фокуса на новую строку
            DataGridTable.IsReadOnly = false;       // Разрешение редактирования для новой строки
            EditBtn.Content = "Сохранить";          // Изменение текста кнопки
        }

        private void DocSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = DocSearchBox.Text.ToLower(); // Получение текста поиска (в нижнем регистре)

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поле поиска пустое, показываем все документы
                DataGridTable.ItemsSource = _allDocuments;
            }
            else
            {
                // Фильтрация документов по всем полям
                var filteredDocuments = _allDocuments
                    .Where(doc =>
                        // Проверка всех полей (преобразование в строку и нижний регистр)
                        doc.Receipt_Date.ToString("dd.MM.yyyy").ToLower().Contains(searchText) ||
                        (doc.Number?.ToLower().Contains(searchText) == true) ||
                        (doc.Title?.ToLower().Contains(searchText) == true) ||
                        (doc.Annotation?.ToLower().Contains(searchText) == true) ||
                        (doc.Source?.ToLower().Contains(searchText) == true) ||
                        (doc.Copies_Count.ToString().ToLower().Contains(searchText)) ||
                        (doc.Storage_Type?.ToLower().Contains(searchText) == true)
                    )
                    .ToList();
                DataGridTable.ItemsSource = filteredDocuments;
            }
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            DocSearchBox.Text = string.Empty; // Очистка поля поиска
            DataGridTable.ItemsSource = _allDocuments; // Восстановление полного списка
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

            // Если клик был на пустом месте и DocSearchBox в фокусе, снимаем фокус
            if (isEmptySpace && Keyboard.FocusedElement == DocSearchBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Esc или Enter
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // Если DocSearchBox в фокусе, снимаем фокус
                if (Keyboard.FocusedElement == DocSearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
            }
        }
    }
}