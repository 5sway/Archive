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
using System.Data.Entity;

namespace ArchiveApp
{
    /// <summary>
    /// Логика взаимодействия для UserPage.xaml
    /// </summary>
    public partial class UserPage : Page
    {
        private bool isAddingNewRow = false;
        private User newUser;
        public UserPage()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadData();
        }

        private void LoadData()
        {
            // Загрузка данных пользователей из базы данных с включением связанных ролей
            using (var context = new ArchiveBaseEntities())
            {
                // Установка источника данных для DataGrid
                DataGridTable.ItemsSource = context.User.Include(u => u.Role).ToList();
            }
            // Загрузка списка ролей для выпадающих списков
            LoadRoles();
            // Установка режима только для чтения по умолчанию
            DataGridTable.IsReadOnly = true;
        }

        private List<Role> _roles;
        public List<Role> Roles
        {
            get { return _roles; }
            set
            {
                _roles = value;
            }
        }

        private void LoadRoles()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Roles = context.Role.ToList();
            }
        }

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            // Получение выбранных для удаления пользователей
            var UsersForRemoving = DataGridTable.SelectedItems.Cast<User>().ToList();

            // Проверка, что хотя бы один элемент выбран
            if (UsersForRemoving.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Подтверждение удаления
            if (MessageBox.Show($"Вы точно хотите удалить {UsersForRemoving.Count} элементов?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        // Удаление всех связанных записей и самого пользователя
                        foreach (var usr in UsersForRemoving)
                        {
                            var usrToRemove = context.User.Find(usr.Id);
                            if (usrToRemove != null)
                            {
                                // Удаление связанных регистрационных карточек
                                context.Registration_Card.RemoveRange(usrToRemove.Registration_Card);
                                // Удаление связанных запросов
                                context.Request.RemoveRange(usrToRemove.Request);
                                // Удаление самого пользователя
                                context.User.Remove(usrToRemove);
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
                // Обработка нового пользователя, если идет добавление
                if (newUser != null && isAddingNewRow)
                {
                    // Проверка обязательных полей
                    if (string.IsNullOrWhiteSpace(newUser.Login) ||
                        string.IsNullOrWhiteSpace(newUser.Password))
                    {
                        RemoveEmptyRow();
                        return;
                    }
                    // Установка связанной роли для нового пользователя
                    newUser.Role = Roles.FirstOrDefault(r => r.Id == newUser.Role_Id);
                    context.User.Add(newUser);
                }

                // Обновление измененных данных существующих пользователей
                foreach (var item in DataGridTable.Items)
                {
                    if (item is User usr && usr != newUser)
                    {
                        var usrToUpdate = context.User.Include(u => u.Role).FirstOrDefault(u => u.Id == usr.Id);
                        if (usrToUpdate != null)
                        {
                            // Обновление всех полей пользователя
                            usrToUpdate.Role_Id = usr.Role_Id;
                            usrToUpdate.Role = Roles.FirstOrDefault(r => r.Id == usr.Role_Id);
                            usrToUpdate.Login = usr.Login;
                            usrToUpdate.Password = usr.Password;
                            usrToUpdate.Name = usr.Name;
                            usrToUpdate.Full_Name = usr.Full_Name;
                            usrToUpdate.First_Name = usr.First_Name;
                            usrToUpdate.Phone_Number = usr.Phone_Number;
                            usrToUpdate.Email = usr.Email;
                        }
                    }
                }

                context.SaveChanges();
            }
            // Сброс флагов и перезагрузка данных
            isAddingNewRow = false;
            newUser = null;
            LoadData();
        }

        private void RemoveEmptyRow()
        {
            var items = DataGridTable.ItemsSource as List<User>;
            if (items != null)
            {
                items.Remove(newUser);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            isAddingNewRow = false;
            newUser = null;
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
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex], dataGrid.Columns[nextColumnIndex]);
                }
                else if (currentRowIndex < dataGrid.Items.Count - 1)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.Items[currentRowIndex + 1], dataGrid.Columns[0]);
                }

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

            // Создание нового пользователя с дефолтными значениями
            newUser = new User
            {
                Role_Id = (int)(Roles.FirstOrDefault()?.Id),
                Login = "",
                Password = "",
                Name = "",
                Full_Name = "",
                First_Name = "",
                Phone_Number = "",
                Email = "",
                Role = Roles.FirstOrDefault()
            };

            // Добавление нового пользователя в коллекцию
            var items = DataGridTable.ItemsSource as List<User>;
            if (items != null)
            {
                items.Add(newUser);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            // Установка фокуса на новую строку
            DataGridTable.SelectedItem = newUser;

            // Блокировка редактирования других строк во время добавления
            foreach (var item in DataGridTable.Items)
            {
                if (item is User usr && usr != newUser)
                {
                    var row = DataGridTable.ItemContainerGenerator.ContainerFromItem(usr) as DataGridRow;
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
                DataGridTable.ItemsSource = ArchiveBaseEntities.GetContext().User.ToList();
            }
        }
    }
}
