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
    /// Логика взаимодействия для DocumentPage.xaml
    /// </summary>
    public partial class DocumentPage : Page
    {
        private bool isAddingNewRow = false;
        private Document newDocument;
        private List<string> _storageTypes;

        public List<string> StorageTypes
        {
            get { return _storageTypes; }
            set { _storageTypes = value; }
        }

        public DocumentPage()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadStorageTypes();
            LoadData();
        }

        private void LoadStorageTypes()
        {
            StorageTypes = new List<string>
        {
            "Бумажный",
            "Электронный"
        };
        }

        private void LoadData()
        {
            // Загрузка данных документов из базы данных
            using (var context = new ArchiveBaseEntities())
            {
                // Установка источника данных для DataGrid
                DataGridTable.ItemsSource = context.Document.ToList();
            }
            // Установка режима только для чтения по умолчанию
            DataGridTable.IsReadOnly = true;
        }


        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            // Получение выбранных для удаления документов
            var documentsForRemoving = DataGridTable.SelectedItems.Cast<Document>().ToList();

            // Проверка, что хотя бы один элемент выбран
            if (documentsForRemoving.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Подтверждение удаления
            if (MessageBox.Show($"Вы точно хотите удалить {documentsForRemoving.Count} элементов?",
                    "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        // Удаление каждого выбранного документа и связанных данных
                        foreach (var doc in documentsForRemoving)
                        {
                            var docToRemove = context.Document.Find(doc.Id);
                            if (docToRemove != null)
                            {
                                // Удаление связанных регистрационных карточек
                                context.Registration_Card.RemoveRange(docToRemove.Registration_Card);
                                // Удаление связанных запросов
                                context.Request.RemoveRange(docToRemove.Request);
                                // Удаление самого документа
                                context.Document.Remove(docToRemove);
                            }
                        }
                        context.SaveChanges();
                    }
                    MessageBox.Show("Данные удалены!");
                    // Перезагрузка данных после удаления
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
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
            // Сохранение изменений в базе данных
            using (var context = new ArchiveBaseEntities())
            {
                // Обработка добавления нового документа
                if (newDocument != null && isAddingNewRow)
                {
                    // Проверка обязательных полей
                    if (string.IsNullOrWhiteSpace(newDocument.Title) ||
                        string.IsNullOrWhiteSpace(newDocument.Number) ||
                        string.IsNullOrWhiteSpace(newDocument.Source) ||
                        string.IsNullOrWhiteSpace(newDocument.Storage_Type))
                    {
                        RemoveEmptyRow();
                        return;
                    }

                    // Добавление нового документа
                    context.Document.Add(newDocument);
                }

                // Обновление существующих документов
                foreach (var item in DataGridTable.Items)
                {
                    if (item is Document doc && doc != newDocument)
                    {
                        var docToUpdate = context.Document.Find(doc.Id);
                        if (docToUpdate != null)
                        {
                            // Обновление всех полей документа
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
                context.SaveChanges();
            }

            // Сброс флагов и перезагрузка данных
            isAddingNewRow = false;
            newDocument = null;
            LoadData();
        }

        private void RemoveEmptyRow()
        {
            var items = DataGridTable.ItemsSource as List<Document>;
            if (items != null)
            {
                items.Remove(newDocument);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            isAddingNewRow = false;
            newDocument = null;
            DataGridTable.IsReadOnly = true;
            EditBtn.Content = "Изменить";
        }
        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Обработка нажатия Enter для удобного перехода между ячейками
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Предотвращаем стандартное поведение

                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var currentCell = dataGrid.CurrentCell;
                if (currentCell.Column == null) return;

                // Определяем следующую ячейку для перехода
                int currentColumnIndex = currentCell.Column.DisplayIndex;
                int nextColumnIndex = currentColumnIndex + 1;
                int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item);

                // Логика перехода между ячейками и строками
                if (nextColumnIndex < dataGrid.Columns.Count)
                {
                    // Переход в следующую колонку
                    dataGrid.CurrentCell = new DataGridCellInfo(
                        dataGrid.Items[currentRowIndex],
                        dataGrid.Columns[nextColumnIndex]);
                }
                else if (currentRowIndex < dataGrid.Items.Count - 1)
                {
                    // Переход на следующую строку
                    dataGrid.CurrentCell = new DataGridCellInfo(
                        dataGrid.Items[currentRowIndex + 1],
                        dataGrid.Columns[0]);
                }

                // Запуск редактирования новой ячейки
                dataGrid.Dispatcher.InvokeAsync(() =>
                {
                    dataGrid.BeginEdit();
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            // Защита от повторного добавления
            if (isAddingNewRow)
                return;

            isAddingNewRow = true;

            // Создание нового документа с дефолтными значениями
            newDocument = new Document
            {
                Receipt_Date = DateTime.Now, // Текущая дата
                Number = "", // Пустой номер
                Title = "", // Пустое название
                Source = "", // Пустой источник
                Copies_Count = 0, // 0 копий по умолчанию
                Annotation = "", // Пустая аннотация
                Storage_Type = StorageTypes.FirstOrDefault() // Первый тип хранения из списка
            };

            // Добавление нового документа в коллекцию
            var items = DataGridTable.ItemsSource as List<Document>;
            if (items != null)
            {
                items.Add(newDocument);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            // Установка фокуса на новую строку
            DataGridTable.SelectedItem = newDocument;

            // Блокировка редактирования других строк во время добавления
            foreach (var item in DataGridTable.Items)
            {
                if (item is Document doc && doc != newDocument)
                {
                    var row = DataGridTable.ItemContainerGenerator.ContainerFromItem(doc) as DataGridRow;
                    if (row != null)
                    {
                        row.IsEnabled = false;
                    }
                }
            }

            // Переключение в режим редактирования
            DataGridTable.IsReadOnly = false;
            EditBtn.Content = "Сохранить";
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                ArchiveBaseEntities.GetContext().ChangeTracker.Entries().ToList().ForEach(p => p.Reload());
                DataGridTable.ItemsSource = ArchiveBaseEntities.GetContext().Document.ToList();
            }
        }
    }
}