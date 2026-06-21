using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class DocumentPage : Page
    {
        private bool isAddingNewRow = false;
        private Document newDocument;
        private List<string> _storageTypes;
        private string currentUserRole = UserData.CurrentUserRole;
        private List<Document> _allDocuments;

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
            if (currentUserRole == "Делопроизводитель")
            {
                DelBtn.Visibility = Visibility.Collapsed;
                AddBtn.Visibility = Visibility.Collapsed;
                EditBtn.Visibility = Visibility.Collapsed;
                return;
            }
            DataGridTable.BeginningEdit += DataGridTable_BeginningEdit;
        }

        private void LoadStorageTypes()
        {
            StorageTypes = new List<string>
            {
                "Бумажный",
                "Электронный"
            };
        }

        private void AttachScanBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTable.SelectedItem is Document selectedDoc)
            {
                string path = DocumentAttachmentService.AttachFile(selectedDoc.Id);
                if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("Скан успешно прикреплён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите документ для прикрепления скана.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ViewScansBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTable.SelectedItem is Document selectedDoc)
            {
                ShowAttachmentsWindow(selectedDoc.Id);
            }
            else
            {
                MessageBox.Show("Выберите документ.", "Внимание");
            }
        }

        private void DataGridTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridTable.SelectedItem is Document selectedDoc)
            {
                ShowAttachmentsWindow(selectedDoc.Id);
            }
        }

        /// <summary>
        /// Отображает окно со списком прикреплённых сканов для выбранного документа.
        /// Поддерживает открытие файлов двойным кликом и удаление по клавише Delete.
        /// </summary>
        private void ShowAttachmentsWindow(int documentId)
        {
            var attachments = DocumentAttachmentService.GetAttachments(documentId);

            if (!attachments.Any())
            {
                MessageBox.Show("К данному документу сканы не прикреплены.", "Информация");
                return;
            }

            var window = new Window
            {
                Title = $"Сканы документа ID: {documentId}",
                Width = 680,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            var listBox = new ListBox
            {
                Margin = new Thickness(10),
                FontSize = 13
            };

            foreach (var att in attachments)
            {
                var item = new ListBoxItem
                {
                    Content = $"{att.FileName}  |  {att.UploadDate:dd.MM.yyyy HH:mm}",
                    Tag = att.FilePath,
                    Padding = new Thickness(5)
                };
                listBox.Items.Add(item);
            }
            listBox.MouseDoubleClick += (s, args) =>
            {
                if (listBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string path)
                {
                    DocumentAttachmentService.OpenAttachment(path);
                }
            };
            listBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Delete && listBox.SelectedItem is ListBoxItem selectedItem)
                {
                    if (selectedItem.Tag is string filePath)
                    {
                        DeleteAttachment(filePath, documentId, listBox);
                    }
                    e.Handled = true;
                }
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            stack.Children.Add(new TextBlock
            {
                Text = $"Прикреплённые сканы: {attachments.Count} шт.",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 5),
                FontSize = 14
            });

            stack.Children.Add(listBox);

            var hint = new TextBlock
            {
                Text = "• Двойной клик — открыть файл\n• Выделить файл и нажать Delete — удалить",
                Margin = new Thickness(10),
                FontSize = 12,
                Foreground = Brushes.Gray
            };
            stack.Children.Add(hint);

            window.Content = stack;
            window.ShowDialog();
        }

        private void DeleteAttachment(string filePath, int documentId, ListBox listBox)
        {
            string fileName = Path.GetFileName(filePath);

            if (MessageBox.Show($"Удалить файл?\n\n{fileName}",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    AuditService.Log("Удалён скан документа", "Document",
                        $"Документ ID: {documentId}, Файл: {fileName}");
                    var updatedAttachments = DocumentAttachmentService.GetAttachments(documentId);

                    listBox.Items.Clear();
                    foreach (var att in updatedAttachments)
                    {
                        var item = new ListBoxItem
                        {
                            Content = $"{att.FileName}  |  {att.UploadDate:dd.MM.yyyy HH:mm}",
                            Tag = att.FilePath,
                            Padding = new Thickness(5)
                        };
                        listBox.Items.Add(item);
                    }

                    MessageBox.Show("Файл успешно удалён.", "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewAttachmentsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTable.SelectedItem is Document selectedDoc)
            {
                var attachments = DocumentAttachmentService.GetAttachments(selectedDoc.Id);
                if (attachments.Any())
                {
                    var file = attachments.First();
                    DocumentAttachmentService.OpenAttachment(file.FilePath);
                }
                else
                {
                    MessageBox.Show("К данному документу сканы не прикреплены.", "Информация");
                }
            }
        }
        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                _allDocuments = context.Document.ToList();
                DataGridTable.ItemsSource = _allDocuments;
            }
            DataGridTable.IsReadOnly = true;
        }

        private void DeleteSelectedDocuments()
        {
            var documentsForRemoving = DataGridTable.SelectedItems.Cast<Document>().ToList();
            if (documentsForRemoving.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {documentsForRemoving.Count} элементов?",
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        foreach (var doc in documentsForRemoving)
                        {
                            var docToRemove = context.Document.Find(doc.Id);
                            if (docToRemove != null)
                            {
                                context.Registration_Card.RemoveRange(docToRemove.Registration_Card);
                                context.Request.RemoveRange(docToRemove.Request);
                                context.Document.Remove(docToRemove);
                            }
                            AuditService.Log("Удалён документ", "Document",
                            $"ID: {doc.Id}, Название: {doc.Title}");
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
            DeleteSelectedDocuments();
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

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode();
        }

        /// <summary>
        /// Сохраняет изменения в базе данных, включая добавление нового документа.
        /// Проверяет обязательные поля и корректность количества копий.
        /// При ошибке валидации удаляет пустую строку.
        /// </summary>
        private void SaveChanges()
        {
            using (var context = new ArchiveBaseEntities())
            {
                if (newDocument != null && isAddingNewRow)
                {
                    if (string.IsNullOrWhiteSpace(newDocument.Title) ||
                        string.IsNullOrWhiteSpace(newDocument.Number) ||
                        string.IsNullOrWhiteSpace(newDocument.Source) ||
                        string.IsNullOrWhiteSpace(newDocument.Storage_Type))
                    {
                        RemoveEmptyRow();
                        MessageBox.Show("Обязательные поля не заполнены. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (newDocument.Copies_Count <= 0)
                    {
                        RemoveEmptyRow();
                        MessageBox.Show("Количество копий должно быть больше 0. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    context.Document.Add(newDocument);
                    AuditService.Log("Добавлен документ", "Document",
                    $"Название: {newDocument.Title}, Полка: {newDocument.Shelf_Number}");
                }

                foreach (var item in DataGridTable.Items)
                {
                    if (item is Document doc && doc != newDocument)
                    {
                        var docToUpdate = context.Document.Find(doc.Id);
                        if (docToUpdate != null)
                        {
                            docToUpdate.Number = doc.Number;
                            docToUpdate.Receipt_Date = doc.Receipt_Date;
                            docToUpdate.Title = doc.Title;
                            docToUpdate.Annotation = doc.Annotation;
                            docToUpdate.Source = doc.Source;
                            docToUpdate.Copies_Count = doc.Copies_Count;
                            docToUpdate.Storage_Type = doc.Storage_Type;
                            docToUpdate.Shelf_Number = doc.Shelf_Number;
                        }
                    }
                }
                context.SaveChanges();
            }
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

        private void DataGridTable_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (isAddingNewRow && e.Row.Item == newDocument)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var document = e.Row.Item as Document;
                    if (document != null)
                    {
                        if (string.IsNullOrWhiteSpace(document.Title) ||
                            string.IsNullOrWhiteSpace(document.Number) ||
                            string.IsNullOrWhiteSpace(document.Source) ||
                            string.IsNullOrWhiteSpace(document.Storage_Type) ||
                            string.IsNullOrWhiteSpace(document.Shelf_Number))
                        {
                            RemoveEmptyRow();
                            MessageBox.Show("Обязательные поля не заполнены. Строка удалена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

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

        /// <summary>
        /// Обрабатывает нажатия клавиш в DataGrid:
        /// Delete — удаление выбранных записей,
        /// Enter — переход к следующей ячейке или сохранение строки.
        /// </summary>
        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && currentUserRole != "Делопроизводитель")
            {
                e.Handled = true;
                DeleteSelectedDocuments();
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
                if (currentColumnIndex == totalColumns - 1 && currentUserRole != "Делопроизводитель")
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {ex.Message}");
                    MessageBox.Show("Некорректный формат даты. Пожалуйста, исправьте значение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (isAddingNewRow && e.Row.Item != newDocument && currentUserRole != "Делопроизводитель")
            {
                e.Cancel = true;
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNewRow) return;

            isAddingNewRow = true;
            newDocument = new Document
            {
                Receipt_Date = DateTime.Now,
                Number = "",
                Title = "",
                Source = "",
                Copies_Count = 0,
                Annotation = "",
                Shelf_Number = "",
                Storage_Type = StorageTypes.FirstOrDefault()
            };

            var items = DataGridTable.ItemsSource as List<Document>;
            if (items != null)
            {
                items.Add(newDocument);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            DataGridTable.SelectedItem = newDocument;
            DataGridTable.IsReadOnly = false;
            EditBtn.Content = "Сохранить";
        }

        private void DocSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = DocSearchBox.Text?.ToLower().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                DataGridTable.ItemsSource = _allDocuments;
                return;
            }

            var filtered = _allDocuments.Where(doc =>
                (doc.Title?.ToLower().Contains(searchText) == true) ||
                (doc.Number?.ToLower().Contains(searchText) == true) ||
                (doc.Annotation?.ToLower().Contains(searchText) == true) ||
                (doc.Source?.ToLower().Contains(searchText) == true) ||
                (doc.Shelf_Number?.ToLower().Contains(searchText) == true) ||
                doc.Receipt_Date.ToString("dd.MM.yyyy").Contains(searchText)
            ).ToList();

            DataGridTable.ItemsSource = filtered;
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            DocSearchBox.Text = string.Empty;
            DataGridTable.ItemsSource = _allDocuments;
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

            if (isEmptySpace && Keyboard.FocusedElement == DocSearchBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement == DocSearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }
    }
}