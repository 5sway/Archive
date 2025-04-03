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
            using (var context = new ArchiveBaseEntities())
            {
                DataGridTable.ItemsSource = context.Document.ToList();
            }
            DataGridTable.IsReadOnly = true;
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            var documentsForRemoving = DataGridTable.SelectedItems.Cast<Document>().ToList();

            if (documentsForRemoving.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        return;
                    }

                    context.Document.Add(newDocument);
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
        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                    
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid == null) return;

                var currentCell = dataGrid.CurrentCell;
                if (currentCell.Column == null) return;

                int currentColumnIndex = currentCell.Column.DisplayIndex;
                int nextColumnIndex = currentColumnIndex + 1;
                int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item);

                if (nextColumnIndex < dataGrid.Columns.Count)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(
                    dataGrid.Items[currentRowIndex],
                    dataGrid.Columns[nextColumnIndex]);
                }
                else if (currentRowIndex < dataGrid.Items.Count - 1)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(
                    dataGrid.Items[currentRowIndex + 1],
                    dataGrid.Columns[0]);
                }

                dataGrid.Dispatcher.InvokeAsync(() =>
                {
                    dataGrid.BeginEdit();
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNewRow)
                return;

            isAddingNewRow = true;

            newDocument = new Document
            {
                Receipt_Date = DateTime.Now,
                Number = "",
                Title = "",
                Source = "",
                Copies_Count = 0,
                Annotation = "",
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
