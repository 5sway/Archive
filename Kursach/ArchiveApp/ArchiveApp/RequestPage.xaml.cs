using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Data.Entity;

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

        public RequestPage()
        {
            InitializeComponent();
            DataContext = this;
            Requests = new ObservableCollection<Request>();
            LoadStatusList();
            LoadDocuments();
            LoadUsers();
            LoadData();
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>>
            {
                new KeyValuePair<bool?, string>(true, "Принято"),
                new KeyValuePair<bool?, string>(false, "Отклонено"),
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
            // Загрузка данных о запросах из базы данных
            using (var context = new ArchiveBaseEntities())
            {
                // Включаем связанные данные о пользователях и документах
                var requests = context.Request
                    .Include(r => r.User)
                    .Include(r => r.Document)
                    .ToList();

                // Очищаем текущую коллекцию и заполняем новыми данными
                Requests.Clear();
                foreach (var req in requests)
                {
                    Requests.Add(req);
                }
            }
            // Устанавливаем источник данных для DataGrid
            DataGridTable.ItemsSource = Requests;
            DataGridTable.IsReadOnly = true; // Запрещаем редактирование по умолчанию
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранные для удаления запросы
            var selectedRequests = DataGridTable.SelectedItems.Cast<Request>().ToList();

            // Проверяем, что хотя бы один элемент выбран
            if (selectedRequests.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Запрашиваем подтверждение удаления
            if (MessageBox.Show($"Вы точно хотите удалить {selectedRequests.Count} элементов?",
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        // Удаляем каждый выбранный запрос
                        foreach (var req in selectedRequests)
                        {
                            var reqToRemove = context.Request.Find(req.Id);
                            if (reqToRemove != null)
                            {
                                context.Request.Remove(reqToRemove);
                            }
                        }
                        context.SaveChanges(); // Сохраняем изменения в БД
                    }
                    MessageBox.Show("Данные удалены!");
                    LoadData(); // Перезагружаем данные после удаления
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
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    // Обработка добавления новой строки
                    if (isAddingNewRow && newRequest != null)
                    {
                        // Проверка обязательных полей
                        if (string.IsNullOrWhiteSpace(newRequest.Reason) ||
                            newRequest.Document_Id == 0)
                        {
                            RemoveEmptyRow();
                            return;
                        }

                        // Установка текущего пользователя и статуса
                        newRequest.User_Id = currentUserId;
                        if (newRequest.Status == null)
                        {
                            newRequest.Status = null;
                        }

                        // Добавление нового запроса
                        context.Request.Add(newRequest);
                        context.SaveChanges();
                    }

                    // Обновление существующих запросов
                    foreach (var req in Requests.Where(r => r.Id != 0))
                    {
                        var reqToUpdate = context.Request.Find(req.Id);
                        if (reqToUpdate != null)
                        {
                            // Обновление всех полей запроса
                            reqToUpdate.Request_Date = req.Request_Date;
                            reqToUpdate.Reason = req.Reason;
                            reqToUpdate.Status = req.Status;
                            reqToUpdate.Document_Id = req.Document_Id;
                        }
                    }
                    context.SaveChanges(); // Сохраняем все изменения
                }

                // Сбрасываем флаги и перезагружаем данные
                isAddingNewRow = false;
                newRequest = null;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveEmptyRow()
        {
            if (newRequest != null && Requests.Contains(newRequest))
            {
                Requests.Remove(newRequest);
            }
            isAddingNewRow = false;
            newRequest = null;
            DataGridTable.IsReadOnly = true;
            EditBtn.Content = "Изменить";
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            // Защита от повторного добавления
            if (isAddingNewRow)
                return;

            isAddingNewRow = true;

            // Создание нового запроса с дефолтными значениями
            newRequest = new Request
            {
                Id = 0, // ID=0 означает новую запись
                Request_Date = DateTime.Now, // Текущая дата
                Reason = "", // Пустое основание
                Status = null, // Статус не установлен
                User_Id = currentUserId, // Текущий пользователь
                Document_Id = 0, // Документ не выбран
                Document = Documents.FirstOrDefault(), // Первый документ по умолчанию
                User = Users.FirstOrDefault(u => u.Id == currentUserId) // Данные текущего пользователя
            };

            // Добавление в коллекцию и настройка UI
            Requests.Add(newRequest);
            DataGridTable.SelectedItem = newRequest;
            DataGridTable.ScrollIntoView(newRequest); // Прокрутка к новой строке
            DataGridTable.IsReadOnly = false; // Разрешаем редактирование
            EditBtn.Content = "Сохранить"; // Меняем текст кнопки
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                LoadData();
            }
        }
    }
}