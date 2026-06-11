using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using System.Windows.Media;

namespace ArchiveApp
{
    public partial class UserPage : Page
    {
        private bool isAddingNewRow = false;
        private User newUser;
        private List<User> _allUsers;
        private List<Role> _roles;

        public List<Role> Roles
        {
            get { return _roles; }
            set { _roles = value; }
        }

        public UserPage()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadData();
            DataGridTable.BeginningEdit += DataGridTable_BeginningEdit;
        }

        private void LoadData()
        {
            using (var context = new ArchiveBaseEntities())
            {
                _allUsers = context.User.Include(u => u.Role).ToList();
                DataGridTable.ItemsSource = _allUsers;
            }
            LoadRoles();
            DataGridTable.IsReadOnly = true;
        }

        private void LoadRoles()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Roles = context.Role.ToList();
            }
        }

        private void DeleteSelectedUsers()
        {
            var usersForRemoving = DataGridTable.SelectedItems.Cast<User>().ToList();
            if (usersForRemoving.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один элемент для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {usersForRemoving.Count} элементов?",
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ArchiveBaseEntities())
                    {
                        foreach (var usr in usersForRemoving)
                        {
                            var usrToRemove = context.User.Find(usr.Id);
                            if (usrToRemove != null)
                            {
                                context.Registration_Card.RemoveRange(usrToRemove.Registration_Card);
                                context.Request.RemoveRange(usrToRemove.Request);
                                context.User.Remove(usrToRemove);
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

        private void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedUsers();
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

        private void SaveChanges()
        {
            using (var context = new ArchiveBaseEntities())
            {
                if (newUser != null && isAddingNewRow)
                {
                    if (string.IsNullOrWhiteSpace(newUser.Login) ||
                        string.IsNullOrWhiteSpace(newUser.Password))
                    {
                        RemoveEmptyRow();
                        MessageBox.Show("Логин и пароль обязательны для заполнения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    newUser.Role = Roles.FirstOrDefault(r => r.Id == newUser.Role_Id);
                    context.User.Add(newUser);
                }

                foreach (var item in DataGridTable.Items)
                {
                    if (item is User usr && usr != newUser)
                    {
                        var usrToUpdate = context.User.Include(u => u.Role).FirstOrDefault(u => u.Id == usr.Id);
                        if (usrToUpdate != null)
                        {
                            usrToUpdate.Role_Id = usr.Role_Id;
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
                context.SaveChanges();
            }
            isAddingNewRow = false;
            newUser = null;
            LoadData();
        }

        private void RemoveEmptyRow()
        {
            var items = DataGridTable.ItemsSource as List<User>;
            if (items != null && newUser != null)
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
            if (e.Key == Key.Delete)
            {
                e.Handled = true;
                DeleteSelectedUsers();
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

                // Если это последний столбец (Email), сохраняем изменения
                if (currentColumnIndex == totalColumns - 1)
                {
                    try
                    {
                        dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        ToggleEditMode();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                // Обычная обработка Enter для перехода между ячейками
                try
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка ввода данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (isAddingNewRow && e.Row.Item != newUser)
            {
                // Отменяем редактирование для всех строк, кроме новой
                e.Cancel = true;
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isAddingNewRow) return;

            isAddingNewRow = true;
            newUser = new User
            {
                Role_Id = Roles.FirstOrDefault()?.Id ?? 0,
                Login = "",
                Password = "",
                Name = "",
                Last_Name = "",
                First_Name = "",
                Phone_Number = "",
                Email = "",
                Role = Roles.FirstOrDefault()
            };

            var items = DataGridTable.ItemsSource as List<User>;
            if (items != null)
            {
                items.Add(newUser);
                DataGridTable.ItemsSource = null;
                DataGridTable.ItemsSource = items;
            }

            DataGridTable.SelectedItem = newUser;
            DataGridTable.IsReadOnly = false;
            EditBtn.Content = "Сохранить";
        }

        private void UserSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = UserSearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                DataGridTable.ItemsSource = _allUsers;
            }
            else
            {
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
            UserSearchBox.Text = string.Empty;
            DataGridTable.ItemsSource = _allUsers;
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

            if (isEmptySpace && Keyboard.FocusedElement == UserSearchBox)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement == UserSearchBox)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }
    }
}