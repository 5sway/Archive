using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.Entity;

namespace ArchiveApp
{
    public partial class StorageControlPage : Page
    {
        private List<StorageControlItem> _allControlItems = new List<StorageControlItem>();
        private List<StorageControlItem> _deletedItems = new List<StorageControlItem>();

        public StorageControlPage()
        {
            InitializeComponent();
            LoadControlData();
            StartRecycleBinCleanupTimer();
        }

        /// <summary>
        /// Таймер для автоматической очистки корзины раз в минуту
        /// </summary>
        private void StartRecycleBinCleanupTimer()
        {
            var timer = new System.Timers.Timer(60000); // 1 минута
            timer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    CleanupRecycleBin();
                });
            };
            timer.Start();
        }

        /// <summary>
        /// Загружает данные о документах и рассчитывает сроки хранения.
        /// Определяет статус каждого документа: "В норме", "Заканчивается", "Просрочен" или "В корзине".
        /// </summary>
        private void LoadControlData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                var docs = context.Document.ToList();
                _allControlItems.Clear();
                _deletedItems.Clear();

                foreach (var doc in docs)
                {
                    // Проверяем, помечен ли документ как удалённый (используем поле Shelf_Number для хранения метки)
                    bool isDeleted = doc.Shelf_Number?.StartsWith("[УДАЛЁН]") == true;
                    DateTime? deletionDate = null;

                    if (isDeleted && doc.Shelf_Number.Contains("до "))
                    {
                        string datePart = doc.Shelf_Number.Substring(doc.Shelf_Number.IndexOf("до ") + 3);
                        if (DateTime.TryParse(datePart, out DateTime parsedDate))
                        {
                            deletionDate = parsedDate;
                        }
                    }

                    // Если документ в корзине, показываем его отдельно
                    if (isDeleted && deletionDate.HasValue)
                    {
                        string datePart = DateTime.Now > deletionDate.Value ? "Удалён (окончательно)" : "В корзине";
                        _deletedItems.Add(new StorageControlItem
                        {
                            Id = doc.Id,
                            Title = doc.Title,
                            Storage_Type = doc.Storage_Type ?? "Не указан",
                            Receipt_Date = doc.Receipt_Date,
                            StorageTerm = doc.Shelf_Number,
                            Status = datePart,
                            EndDate = deletionDate.Value,
                            Source = doc.Source,
                            IsDeleted = true,
                            DeletionDate = deletionDate.Value
                        });
                        continue;
                    }

                    int termYears = GetStorageTermYears(doc.Source, doc.Receipt_Date);
                    DateTime endDate = doc.Receipt_Date.AddYears(termYears);

                    string docStatus = DateTime.Now > endDate ? "Просрочен" :
                                   (endDate - DateTime.Now).Days < 365 ? "Заканчивается" : "В норме";

                    string yearWord = GetYearWord(termYears);

                    _allControlItems.Add(new StorageControlItem
                    {
                        Id = doc.Id,
                        Title = doc.Title,
                        Storage_Type = doc.Storage_Type ?? "Не указан",
                        Receipt_Date = doc.Receipt_Date,
                        StorageTerm = $"{termYears} {yearWord} (до {endDate:dd.MM.yyyy})",
                        Status = docStatus,
                        EndDate = endDate,
                        Source = doc.Source,
                        IsDeleted = false,
                        DeletionDate = null
                    });
                }

                RefreshGrid();
            }
        }

        private void RefreshGrid()
        {
            // Показываем сначала активные документы, потом корзину
            var activeItems = _allControlItems.OrderBy(x => x.Status == "Просрочен" ? 0 : x.Status == "Заканчивается" ? 1 : 2);
            var deletedItems = _deletedItems.OrderByDescending(x => x.DeletionDate);

            var combined = activeItems.Concat(deletedItems).ToList();
            ControlDataGrid.ItemsSource = combined;
        }

        /// <summary>
        /// Определяет срок хранения в зависимости от учреждения и даты.
        /// </summary>
        private int GetStorageTermYears(string source, DateTime receiptDate)
        {
            if (string.IsNullOrEmpty(source)) return 5;

            bool isSchool = source.Contains("Школа") || source.Contains("СОШ");
            bool isCollege = source.Contains("Колледж") || source.Contains("ВАГПК");

            if (isSchool)
            {
                return 3;
            }
            else if (isCollege)
            {
                if (receiptDate.Year >= 2003)
                    return 50;
                else
                    return 75;
            }

            return 5;
        }

        private string GetYearWord(int years)
        {
            if (years < 0) return "лет";

            int lastDigit = years % 10;
            int lastTwoDigits = years % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 14)
                return "лет";

            if (lastDigit == 1)
                return "год";
            else if (lastDigit >= 2 && lastDigit <= 4)
                return "года";
            else
                return "лет";
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadControlData();
        }

        /// <summary>
        /// Автоматическая очистка корзины (удаление документов, срок которых истёк)
        /// </summary>
        private void CleanupRecycleBin()
        {
            var toDeletePermanently = _deletedItems
                .Where(x => x.DeletionDate.HasValue && DateTime.Now > x.DeletionDate.Value)
                .ToList();

            if (toDeletePermanently.Any())
            {
                foreach (var item in toDeletePermanently)
                {
                    DeleteDocumentPermanentlyFromDb(item.Id);
                    _deletedItems.Remove(item);
                }
                RefreshGrid();
            }
        }

        /// <summary>
        /// Удаляет документ из БД (безвозвратно)
        /// </summary>
        private void DeleteDocumentPermanentlyFromDb(int documentId)
        {
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    var doc = context.Document.Find(documentId);
                    if (doc != null)
                    {
                        context.Registration_Card.RemoveRange(doc.Registration_Card);
                        context.Request.RemoveRange(doc.Request);
                        context.Document.Remove(doc);
                        context.SaveChanges();

                        AuditService.Log("Окончательное удаление документа", "Document",
                            $"ID: {documentId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления: {ex.Message}");
            }
        }

        /// <summary>
        /// Формирует акт уничтожения для документов с просроченным сроком хранения.
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
            }
        }

        private void ShowDestructionAct(List<StorageControlItem> documents)
        {
            var window = new Window
            {
                Title = "Акт об уничтожении документов",
                Width = 900,
                Height = 700,
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
                Text = "В соответствии с Перечнем типовых архивных документов, утвержденным приказом Росархива,",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "следующие документы подлежат уничтожению:",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15)
            });

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddTableHeader(grid, 0, 0, "№");
            AddTableHeader(grid, 0, 1, "Наименование документа");
            AddTableHeader(grid, 0, 2, "Дата поступления");
            AddTableHeader(grid, 0, 3, "Учреждение");
            AddTableHeader(grid, 0, 4, "Тип хранения");

            int row = 1;
            foreach (var doc in documents)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTableCell(grid, row, 0, row.ToString());
                AddTableCell(grid, row, 1, doc.Title);
                AddTableCell(grid, row, 2, doc.Receipt_Date.ToString("dd.MM.yyyy"));
                AddTableCell(grid, row, 3, doc.Source ?? "—");
                AddTableCell(grid, row, 4, doc.Storage_Type ?? "—");
                row++;
            }

            stack.Children.Add(grid);

            stack.Children.Add(new TextBlock
            {
                Text = $"\nВсего документов к уничтожению: {documents.Count}",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Основание: истечение установленного срока хранения.",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            // Кнопка уничтожения
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var deleteBtn = new Button
            {
                Content = $"Уничтожить {documents.Count} документов",
                Width = 250,
                Height = 40,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff9999")),
                FontWeight = FontWeights.Bold
            };

            var closeBtn = new Button
            {
                Content = "Закрыть",
                Width = 120,
                Height = 40,
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bae3e8"))
            };

            deleteBtn.Click += (s, e) =>
            {
                if (MessageBox.Show($"Вы уверены, что хотите уничтожить {documents.Count} документов?\n\n" +
                    "Документы будут помещены в корзину на 7 дней. За это время их можно восстановить.\n\n" +
                    "По истечении 7 дней документы будут удалены безвозвратно.",
                    "Подтверждение уничтожения", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    MoveToRecycleBin(documents);
                    window.Close();
                    LoadControlData();
                }
            };

            closeBtn.Click += (s, e) => window.Close();

            buttonPanel.Children.Add(deleteBtn);
            buttonPanel.Children.Add(closeBtn);
            stack.Children.Add(buttonPanel);

            stack.Children.Add(new TextBlock
            {
                Text = $"Составил: {UserData.CurrentUserName ?? "Специалист архива"}",
                FontSize = 14,
                Margin = new Thickness(0, 15, 0, 5)
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

        /// <summary>
        /// Помещает документы в корзину на 7 дней
        /// </summary>
        private void MoveToRecycleBin(List<StorageControlItem> documents)
        {
            using (var context = new ArchiveBaseEntities())
            {
                foreach (var item in documents)
                {
                    var doc = context.Document.Find(item.Id);
                    if (doc != null)
                    {
                        DateTime deletionDate = DateTime.Now.AddDays(7);
                        doc.Shelf_Number = $"[УДАЛЁН] до {deletionDate:dd.MM.yyyy}";

                        AuditService.Log("Помещение документа в корзину", "Document",
                            $"ID: {item.Id}, Название: {item.Title}, Удаление: {deletionDate:dd.MM.yyyy}");
                    }
                }
                context.SaveChanges();
            }

            MessageBox.Show($"{documents.Count} документов помещены в корзину.\n" +
                "Они будут храниться 7 дней. За это время их можно восстановить.",
                "Выполнено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddTableHeader(Grid grid, int row, int column, string text)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Background = Brushes.LightGray
            };

            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(5),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        private void AddTableCell(Grid grid, int row, int column, string text)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            var tb = new TextBlock
            {
                Text = text,
                Padding = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }

        private void ControlDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ControlDataGrid.SelectedItem is StorageControlItem item)
            {
                if (item.IsDeleted)
                {
                    ShowRestoreOrDeleteMenu(item);
                }
                else
                {
                    ShowAddStorageTermWindow(item);
                }
            }
        }

        /// <summary>
        /// Открывает окно добавления срока хранения для активных документов
        /// </summary>
        private void ShowAddStorageTermWindow(StorageControlItem item)
        {
            var window = new Window
            {
                Title = "Изменение срока хранения",
                Width = 450,
                Height = 425,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var mainStack = new StackPanel { Margin = new Thickness(20) };

            var infoBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0")),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Документ: {item.Title}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Текущий срок: {item.StorageTerm}",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 0)
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Текущий статус: {item.Status}",
                FontSize = 12,
                Foreground = item.Status == "Просрочен" ? Brushes.Red :
                            item.Status == "Заканчивается" ? Brushes.Orange : Brushes.Green
            });

            infoBorder.Child = infoStack;
            mainStack.Children.Add(infoBorder);

            mainStack.Children.Add(new TextBlock
            {
                Text = "Выберите действие:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var quickButtonsPanel = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            var btn3Years = CreateQuickButton("+3 года", 3);
            var btn5Years = CreateQuickButton("+5 лет", 5);
            var btn10Years = CreateQuickButton("+10 лет", 10);
            var btn50Years = CreateQuickButton("+50 лет", 50);
            var btn75Years = CreateQuickButton("+75 лет", 75);

            quickButtonsPanel.Children.Add(btn3Years);
            quickButtonsPanel.Children.Add(btn5Years);
            quickButtonsPanel.Children.Add(btn10Years);
            quickButtonsPanel.Children.Add(btn50Years);
            quickButtonsPanel.Children.Add(btn75Years);

            mainStack.Children.Add(quickButtonsPanel);

            mainStack.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            var manualStack = new StackPanel();
            manualStack.Children.Add(new TextBlock
            {
                Text = "Или укажите количество лет вручную:",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var inputPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var yearsTextBox = new TextBox
            {
                Width = 120,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(5, 0, 0, 0)
            };

            var setYearsBtn = new Button
            {
                Content = "Установить",
                Width = 100,
                Height = 30,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bae3e8"))
            };

            inputPanel.Children.Add(yearsTextBox);
            inputPanel.Children.Add(setYearsBtn);
            manualStack.Children.Add(inputPanel);

            mainStack.Children.Add(manualStack);

            var closeBtn = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            mainStack.Children.Add(closeBtn);

            btn3Years.Click += (s, e) => { ExtendStorageTerm(item, 3); window.Close(); };
            btn5Years.Click += (s, e) => { ExtendStorageTerm(item, 5); window.Close(); };
            btn10Years.Click += (s, e) => { ExtendStorageTerm(item, 10); window.Close(); };
            btn50Years.Click += (s, e) => { ExtendStorageTerm(item, 50); window.Close(); };
            btn75Years.Click += (s, e) => { ExtendStorageTerm(item, 75); window.Close(); };

            setYearsBtn.Click += (s, e) =>
            {
                if (int.TryParse(yearsTextBox.Text, out int years) && years > 0)
                {
                    ExtendStorageTerm(item, years);
                    window.Close();
                }
                else
                {
                    MessageBox.Show("Введите корректное положительное число лет!",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            closeBtn.Click += (s, e) => window.Close();

            window.Content = mainStack;
            window.ShowDialog();
        }

        private Button CreateQuickButton(string text, int years)
        {
            var btn = new Button
            {
                Content = text,
                Width = 85,
                Height = 35,
                Margin = new Thickness(3),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bae3e8")),
                Tag = years
            };
            return btn;
        }

        /// <summary>
        /// Показывает меню для восстановления или окончательного удаления документа из корзины
        /// </summary>
        /// <summary>
        /// Показывает меню для восстановления или окончательного удаления документа из корзины
        /// </summary>
        private void ShowRestoreOrDeleteMenu(StorageControlItem item)
        {
            var window = new Window
            {
                Title = "Документ в корзине",
                Width = 400,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock
            {
                Text = $"Документ: {item.Title}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var infoBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f0f0")),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var infoStack = new StackPanel();
            var daysLeft = item.DeletionDate.HasValue ? (int)(item.DeletionDate.Value - DateTime.Now).TotalDays : 0;
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Дата окончательного удаления: {item.DeletionDate:dd.MM.yyyy HH:mm}",
                FontSize = 12
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Осталось дней: {Math.Max(0, daysLeft)}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = daysLeft <= 1 ? Brushes.Red : Brushes.Orange
            });

            infoBorder.Child = infoStack;
            stack.Children.Add(infoBorder);

            var restoreBtn = new Button
            {
                Content = "Восстановить документ",
                Height = 40,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bae3e8")),
                FontWeight = FontWeights.Bold
            };

            var deleteNowBtn = new Button
            {
                Content = "Удалить окончательно (сейчас)",
                Height = 40,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff9999")),
                FontWeight = FontWeights.Bold
            };

            var closeBtn = new Button
            {
                Content = "Закрыть",
                Height = 30,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            restoreBtn.Click += (s, e) =>
            {
                RestoreDocument(item);
                window.Close();
                LoadControlData();
            };

            deleteNowBtn.Click += (s, e) =>
            {
                if (MessageBox.Show($"УДАЛИТЬ ОКОНЧАТЕЛЬНО?\n\n{item.Title}\n\nДействие необратимо!",
                    "ВНИМАНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    DeleteDocumentPermanentlyFromDb(item.Id);
                    _deletedItems.Remove(item);
                    window.Close();
                    LoadControlData();
                    MessageBox.Show("Документ удалён окончательно.", "Выполнено");
                }
            };

            closeBtn.Click += (s, e) => window.Close();

            stack.Children.Add(restoreBtn);
            stack.Children.Add(deleteNowBtn);
            stack.Children.Add(closeBtn);

            window.Content = stack;
            window.ShowDialog();
        }

        /// <summary>
        /// Восстанавливает документ из корзины
        /// </summary>
        private void RestoreDocument(StorageControlItem item)
        {
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    var doc = context.Document.Find(item.Id);
                    if (doc != null)
                    {
                        // Очищаем метку удаления
                        doc.Shelf_Number = "";

                        // Показываем окно для установки нового срока
                        context.SaveChanges();

                        AuditService.Log("Восстановление документа из корзины", "Document",
                            $"ID: {item.Id}, Название: {item.Title}");

                        MessageBox.Show("Документ восстановлен из корзины.\n" +
                            "Теперь вы можете установить для него новый срок хранения.",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtendStorageTerm(StorageControlItem item, int years)
        {
            if (years <= 0)
            {
                MessageBox.Show("Количество лет должно быть положительным числом!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string yearWord = GetYearWord(years);

            using (var context = new ArchiveBaseEntities())
            {
                var doc = context.Document.Find(item.Id);
                if (doc != null)
                {
                    // Обновляем дату поступления (сдвигаем, чтобы изменить срок)
                    doc.Receipt_Date = doc.Receipt_Date.AddYears(years);
                    context.SaveChanges();
                }
            }

            // Обновляем локальные данные
            item.EndDate = item.EndDate.AddYears(years);
            item.StorageTerm = $"Продлён на {years} {yearWord} (до {item.EndDate:dd.MM.yyyy})";

            if (DateTime.Now > item.EndDate)
                item.Status = "Просрочен";
            else if ((item.EndDate - DateTime.Now).Days < 365)
                item.Status = "Заканчивается";
            else
                item.Status = "В норме";

            RefreshGrid();

            AuditService.Log("Продление срока хранения", "Document",
                $"Документ ID: {item.Id}, Добавлено {years} {yearWord}, Новая дата: {item.EndDate:dd.MM.yyyy}");

            MessageBox.Show($"Срок хранения продлён на {years} {yearWord}.\n" +
                          $"Новая дата окончания: {item.EndDate:dd.MM.yyyy}\n" +
                          $"Новый статус: {item.Status}",
                          "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddStorageTermBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ControlDataGrid.SelectedItem is StorageControlItem selectedItem)
            {
                if (selectedItem.IsDeleted)
                {
                    ShowRestoreOrDeleteMenu(selectedItem);
                }
                else
                {
                    ShowAddStorageTermWindow(selectedItem);
                }
            }
            else
            {
                MessageBox.Show("Выберите документ!",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        public string Source { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionDate { get; set; }
    }
}
