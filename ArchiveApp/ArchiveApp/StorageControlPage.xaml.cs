using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class StorageControlPage : Page
    {
        private List<StorageControlItem> _allControlItems = new List<StorageControlItem>();

        public StorageControlPage()
        {
            InitializeComponent();
            LoadControlData();
        }

        /// <summary>
        /// Загружает данные о документах и рассчитывает сроки хранения.
        /// Определяет статус каждого документа: "В норме", "Заканчивается" или "Просрочен".
        /// Срок хранения зависит от типа носителя: 5 лет для электронных, 10 лет для бумажных.
        /// </summary>
        private void LoadControlData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                var docs = context.Document.ToList();
                _allControlItems.Clear();

                foreach (var doc in docs)
                {
                    int termYears = GetStorageTermYears(doc.Storage_Type);
                    DateTime endDate = doc.Receipt_Date.AddYears(termYears);

                    string status = DateTime.Now > endDate ? "Просрочен" :
                                   (endDate - DateTime.Now).Days < 365 ? "Заканчивается" : "В норме";

                    _allControlItems.Add(new StorageControlItem
                    {
                        Id = doc.Id,
                        Title = doc.Title,
                        Storage_Type = doc.Storage_Type ?? "Не указан",
                        Receipt_Date = doc.Receipt_Date,
                        StorageTerm = $"{termYears} лет (до {endDate:dd.MM.yyyy})",
                        Status = status,
                        EndDate = endDate
                    });
                }

                RefreshGrid();
            }
        }

        private void RefreshGrid()
        {
            ControlDataGrid.ItemsSource = _allControlItems
                .OrderBy(x => x.Status == "Просрочен" ? 0 : x.Status == "Заканчивается" ? 1 : 2)
                .ToList();
        }

        private int GetStorageTermYears(string storageType)
        {
            if (string.IsNullOrEmpty(storageType)) return 5;
            return storageType.Contains("Электронный") ? 5 : 10;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadControlData();
        }

        /// <summary>
        /// Формирует акт уничтожения для документов с просроченным сроком хранения.
        /// Создаёт отдельное окно с таблицей документов, подлежащих уничтожению.
        /// </summary>
        private void CreateActBtn_Click(object sender, RoutedEventArgs e)
        {
            var overdueDocs = _allControlItems.Where(x => x.Status == "Просрочен").ToList();

            if (!overdueDocs.Any())
            {
                MessageBox.Show("Нет документов с просроченным сроком хранения для формирования акта.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Сформировать акт уничтожения для {overdueDocs.Count} документов?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ShowDestructionAct(overdueDocs);

                AuditService.Log("Формирование акта уничтожения", "Document",
                    $"Количество документов: {overdueDocs.Count}");
            }
        }

        private void ShowDestructionAct(List<StorageControlItem> documents)
        {
            var window = new Window
            {
                Title = "Акт об уничтожении документов",
                Width = 800,
                Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            var scroll = new ScrollViewer { Margin = new Thickness(20) };
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = "АКТ\nоб уничтожении документов с истекшим сроком хранения",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Дата составления: {DateTime.Now:dd.MM.yyyy}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "В соответствии с нормативными требованиями по хранению архивных документов,",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15)
            });
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            AddTableHeader(grid, 0, "№");
            AddTableHeader(grid, 1, "Наименование документа");
            AddTableHeader(grid, 2, "Дата поступления");

            int row = 1;
            foreach (var doc in documents)
            {
                AddTableCell(grid, row, 0, row.ToString());
                AddTableCell(grid, row, 1, doc.Title);
                AddTableCell(grid, row, 2, doc.Receipt_Date.ToString("dd.MM.yyyy"));
                row++;
            }

            stack.Children.Add(grid);

            stack.Children.Add(new TextBlock
            {
                Text = "\nДокументы подлежат уничтожению в связи с истечением установленного срока хранения.",
                FontSize = 13,
                Margin = new Thickness(0, 20, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Составил: {UserData.CurrentUserName ?? "Специалист архива"}",
                FontSize = 14,
                Margin = new Thickness(0, 30, 0, 5)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"Дата: {DateTime.Now:dd.MM.yyyy}",
                FontSize = 14
            });

            scroll.Content = stack;
            window.Content = scroll;
            window.ShowDialog();
        }

        private void AddTableHeader(Grid grid, int column, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(5),
                TextAlignment = TextAlignment.Center,
                Background = Brushes.LightGray
            };
            Grid.SetRow(tb, 0);
            Grid.SetColumn(tb, column);
            grid.Children.Add(tb);
        }

        private void AddTableCell(Grid grid, int row, int column, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                Padding = new Thickness(5),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, column);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Children.Add(tb);
        }
        private void ControlDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ControlDataGrid.SelectedItem is StorageControlItem item)
                ShowActionMenu(item);
        }

        /// <summary>
        /// Показывает контекстное меню для управления конкретным документом:
        /// продление срока хранения или полное уничтожение.
        /// </summary>
        private void ShowActionMenu(StorageControlItem item)
        {
            var window = new Window { Title = "Действия с документом", Width = 340, Height = 240, WindowStartupLocation = WindowStartupLocation.CenterScreen };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock { Text = item.Title, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15), TextWrapping = TextWrapping.Wrap });

            var extendBtn = new Button { Content = "Продлить срок хранения (+5 лет)", Height = 40, Margin = new Thickness(0, 0, 0, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bae3e8")) };
            var deleteBtn = new Button { Content = "Уничтожить документ (со всеми связями)", Height = 40, Margin = new Thickness(0, 0, 0, 10), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff9999")) };

            extendBtn.Click += (s, e) => { ExtendStorageTerm(item); window.Close(); };
            deleteBtn.Click += (s, e) => { DeleteDocumentPermanently(item); window.Close(); };

            stack.Children.Add(extendBtn);
            stack.Children.Add(deleteBtn);

            window.Content = stack;
            window.ShowDialog();
        }

        private void ExtendStorageTerm(StorageControlItem item)
        {
            item.EndDate = item.EndDate.AddYears(5);
            item.StorageTerm = $"Продлён: {item.EndDate:dd.MM.yyyy}";
            item.Status = "В норме";
            RefreshGrid();

            AuditService.Log("Продление срока хранения", "Document", $"Документ ID: {item.Id}");
            MessageBox.Show($"Срок хранения продлён.\nНовая дата: {item.EndDate:dd.MM.yyyy}", "Успешно");
        }

        /// <summary>
        /// Полное удаление документа из системы вместе со всеми связанными записями.
        /// Требует подтверждения пользователя, так как действие необратимо.
        /// </summary>
        private void DeleteDocumentPermanently(StorageControlItem item)
        {
            if (MessageBox.Show($"УНИЧТОЖИТЬ документ?\n\n{item.Title}\n\nДействие необратимо!", "ВНИМАНИЕ",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    var doc = context.Document.Find(item.Id);
                    if (doc != null)
                    {
                        context.Registration_Card.RemoveRange(doc.Registration_Card);
                        context.Request.RemoveRange(doc.Request);
                        context.Document.Remove(doc);
                        context.SaveChanges();

                        AuditService.Log("Уничтожение документа", "Document", $"ID: {item.Id}, Название: {item.Title}");
                    }
                }

                _allControlItems.Remove(item);
                RefreshGrid();
                MessageBox.Show("Документ успешно уничтожен.", "Выполнено");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class StorageControlItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Storage_Type { get; set; }
        public DateTime Receipt_Date { get; set; }
        public string StorageTerm { get; set; }
        public string Status { get; set; }
        public DateTime EndDate { get; set; }
    }
}