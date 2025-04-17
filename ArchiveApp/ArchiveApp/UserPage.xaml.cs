using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;

namespace ArchiveApp
{
    public partial class UserPage : Page
    {
        private bool isAddingNewRow = false;        // Флаг добавления новой строки
        private User newUser;                       // Новый пользователь для добавления
        private List<User> _allUsers;               // Поле для хранения полного списка пользователей

        private List<Role> _roles;                  // Список ролей
        public List<Role> Roles
        {
            get { return _roles; }
            set { _roles = value; }
        }

        public UserPage()
        {
            InitializeComponent();                  // Инициализация компонентов страницы
            this.DataContext = this;                // Установка контекста данных
            LoadData();                            // Загрузка данных пользователей
        }

        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                _allUsers = context.User.Include(u => u.Role).ToList(); // Загрузка пользователей с ролями
                DataGridTable.ItemsSource = _allUsers; // Установка источника данных
            }
            LoadRoles();                            // Загрузка списка ролей
            DataGridTable.IsReadOnly = true;        // Установка режима "только чтение"
        }

        private void LoadRoles()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                Roles = context.Role.ToList();      // Загрузка всех ролей
            }
        }

        private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = UserSearchBox.Text.ToLower(); // Получение текста поиска (в нижнем регистре)

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поле поиска пустое, показываем всех пользователей
                DataGridTable.ItemsSource = _allUsers;
            }
            else
            {
                // Фильтрация пользователей по всем полям
                var filteredUsers = _allUsers
                    .Where(user =>
                        (user.Role?.Name?.ToLower().Contains(searchText) == true) ||
                        (user.Login?.ToLower().Contains(searchText) == true) ||
                        (user.Password?.ToLower().Contains(searchText) == true) ||
                        (user.Name?.ToLower().Contains(searchText) == true) ||
                        (user.Last_Name?.ToLower().Contains(searchText) == true) ||
                        (user.First_Name?.ToLower().Contains(searchText) == true) ||
                        (user.Phone_Number?.ToLower().Contains(searchText) == true) ||
                        (user.Email?.ToLower().Contains(searchText) == true)
                    )
                    .ToList();
                DataGridTable.ItemsSource = filteredUsers;
            }
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            UserSearchBox.Text = string.Empty; // Очистка поля поиска
            DataGridTable.ItemsSource = _allUsers; // Восстановление полного списка
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            var UsersForRemoving = DataGridTable.SelectedItems.Cast<User>().ToList(); // Получение выбранных пользователей
            if (UsersForRemoving.Count == 0)        // Проверка наличия выбранных элементов
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {UsersForRemoving.Count} элементов?", // Подтверждение удаления
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                    {
                        foreach (var usr in UsersForRemoving) // Удаление каждого пользователя
                        {
                            var usrToRemove = context.User.Find(usr.Id); // Поиск пользователя в базе
                            if (usrToRemove != null)
                            {
                                context.Registration_Card.RemoveRange(usrToRemove.Registration_Card); // Удаление связанных карточек
                                context.Request.RemoveRange(usrToRemove.Request); // Удаление связанных запросов
                                context.User.Remove(usrToRemove); // Удаление пользователя
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

        private void EditBtn_Click(object sender, RoutedEventArgs e)
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

        private void SaveChanges()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                if (newUser != null && isAddingNewRow) // Добавление нового пользователя
                {
                    if (string.IsNullOrWhiteSpace(newUser.Login) || // Проверка обязательных полей
                        string.IsNullOrWhiteSpace(newUser.Password))
                    {
                        RemoveEmptyRow();           // Удаление пустой строки при ошибке
                        return;
                    }
                    newUser.Role = Roles.FirstOrDefault(r => r.Id == newUser.Role_Id); // Установка роли
                    context.User.Add(newUser);      // Добавление пользователя в базу
                }

                foreach (var item in DataGridTable.Items) // Обновление существующих пользователей
                {
                    if (item is User usr && usr != newUser) // Проверка типа и исключение нового пользователя
                    {
                        var usrToUpdate = context.User.Include(u => u.Role).FirstOrDefault(u => u.Id == usr.Id); // Поиск пользователя
                        if (usrToUpdate != null)    // Обновление полей
                        {
                            usrToUpdate.Role_Id = usr.Role_Id;
                            usrToUpdate.Role = Roles.FirstOrDefault(r => r.Id == usr.Role_Id);
                            usrToUpdate.Login = usr.Login;
                            usrToUpdate.Password = usr.Password;
                            usrToUpdate.Name = usr.Name;
                            usrToUpdate.Last_Name = usr.Last_Name;
                            usrToUpdate.First_Name = usr.First_Name;
                            usrToUpdate.Phone_Number = usr.Phone_Number;
                            usrToUpdate.Email = usr.Email;
                        }
                    }
                }
                context.SaveChanges();              // Сохранение изменений
            }
            isAddingNewRow = false;                 // Сброс флага добавления
            newUser = null;                         // Очистка нового пользователя
            LoadData();                            // Перезагрузка данных
        }

        private void RemoveEmptyRow()
        {
            var items = DataGridTable.ItemsSource as List<User>; // Получение текущего списка
            if (items != null)                      // Удаление пустой строки
            {
                items.Remove(newUser);              // Удаление нового пользователя
                DataGridTable.ItemsSource = null;   // Сброс источника данных
                DataGridTable.ItemsSource = items;  // Переустановка источника данных
            }
            isAddingNewRow = false;                 // Сброс флага добавления
            newUser = null;                         // Очистка нового пользователя
            DataGridTable.IsReadOnly = true;        // Установка режима "только чтение"
            EditBtn.Content = "Изменить";           // Восстановление текста кнопки
        }

        private void DataGridTable_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)                 // Обработка нажатия Enter
            {
                e.Handled = true;                   // Отмена стандартного поведения
                DataGrid dataGrid = sender as DataGrid; // Получение DataGrid
                if (dataGrid == null) return;

                var currentCell = dataGrid.CurrentCell; // Текущая ячейка
                if (currentCell.Column == null) return;

                int currentColumnIndex = currentCell.Column.DisplayIndex; // Индекс текущей колонки
                int nextColumnIndex = currentColumnIndex + 1; // Следующий индекс колонки
                int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item); // Индекс текущей строки

                if (nextColumnIndex < dataGrid.Columns.Count) // Переход к следующей колонке
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

            private void AddBtn_Click(object sender, RoutedEventArgs e)
            {
                if (isAddingNewRow) return;             // Защита от повторного добавления

                isAddingNewRow = true;                  // Установка флага добавления
                newUser = new User                      // Создание нового пользователя
                {
                    Role_Id = (int)(Roles.FirstOrDefault()?.Id), // ID первой роли по умолчанию
                    Login = "",                        // Пустой логин
                    Password = "",                     // Пустой пароль
                    Name = "",                         // Пустое имя
                    Last_Name = "",                    // Пустая фамилия
                    First_Name = "",                   // Пустое отчество
                    Phone_Number = "",                 // Пустой номер телефона
                    Email = "",                        // Пустой email
                    Role = Roles.FirstOrDefault()      // Первая роль по умолчанию
                };

                var items = DataGridTable.ItemsSource as List<User>; // Получение текущего списка
                if (items != null)                      // Добавление нового пользователя
                {
                    items.Add(newUser);
                    DataGridTable.ItemsSource = null;   // Сброс источника данных
                    DataGridTable.ItemsSource = items;  // Переустановка источника данных
                }

                DataGridTable.SelectedItem = newUser;   // Установка фокуса на новую строку

                foreach (var item in DataGridTable.Items) // Блокировка других строк
                {
                    if (item is User usr && usr != newUser)
                    {
                        var row = DataGridTable.ItemContainerGenerator.ContainerFromItem(usr) as DataGridRow;
                        if (row != null) row.IsEnabled = false; // Отключение строки
                    }
                }

                DataGridTable.IsReadOnly = false;       // Разрешение редактирования
                EditBtn.Content = "Сохранить";          // Изменение текста кнопки
            }
    }
}