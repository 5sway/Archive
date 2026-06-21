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
    public partial class RegCardPage : Page
    {
        private bool isEditMode = false;
        private Registration_Card selectedRegCard = null;
        public List<KeyValuePair<bool?, string>> StatusList { get; set; }
        private int currentUserId = UserData.CurrentUserId;
        public List<Document> Documents { get; set; }
        public List<User> Users { get; set; }
        private string currentUserRole = UserData.CurrentUserRole;
        public List<Registration_Card> RegCards { get; set; }

        public RegCardPage()
        {
            InitializeComponent();
            LoadStatusList();
            LoadUsers();
            LoadRegistrationCards();
            LoadDocuments();
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>>
            {
                new KeyValuePair<bool?, string>(true, "Подписан"),
                new KeyValuePair<bool?, string>(false, "Не подписан")
            };
            SignatureСomboBox.ItemsSource = StatusList;
            SignatureСomboBox.DisplayMemberPath = "Value";
            SignatureСomboBox.SelectedValuePath = "Key";
        }

        private void LoadDocuments()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Documents = context.Document.ToList();
                DocumentComboBox.ItemsSource = Documents;
                DocumentComboBox.DisplayMemberPath = "Title";
                DocumentComboBox.SelectedValuePath = "Id";
            }
            DocumentComboBox.SelectionChanged += DocumentComboBox_SelectionChanged;
            if (Documents != null && Documents.Any())
            {
                DocumentComboBox.SelectedItem = Documents.First();
                DocumentComboBox_SelectionChanged(DocumentComboBox, null);
            }
        }

        private void LoadUsers()
        {
            using (var context = new ArchiveBaseEntities())
            {
                Users = context.User.ToList();
            }
        }

        private void LoadRegistrationCards()
        {
            using (var context = new ArchiveBaseEntities())
            {
                RegCards = context.Registration_Card
                    .Include("User")
                    .Include("Document")
                    .ToList();
            }
        }

        /// <summary>
        /// Управляет режимом редактирования регистрационной карты.
        /// Проверяет права доступа: если документ подписан и пользователь не администратор,
        /// редактирование запрещено.
        /// При сохранении проверяет заполненность полей и обновляет запись в БД.
        /// </summary>
        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                if (currentUserRole != "Администратор")
                {
                    if (selectedRegCard != null && selectedRegCard.Signature == true)
                    {
                        MessageBox.Show("Документ уже подписан и не может быть изменен");
                        return;
                    }
                }

                isEditMode = true;
                EditBtn.Content = "Сохранить";
                TitleTextBox.IsReadOnly = false;
                SignatureСomboBox.IsEnabled = true;
                RegistrationDatePicker.IsEnabled = true;

                if (currentUserRole != "Администратор")
                {
                    var currentUser = Users.FirstOrDefault(u => u.Id == currentUserId);
                    SignedByTextBox.Text = currentUser != null ? $"{currentUser.Last_Name} {currentUser.Name} {currentUser.First_Name}" : "Неизвестно";
                }

                if (selectedRegCard == null || selectedRegCard.Signature == false || currentUserRole == "Администратор")
                {
                    RegistrationDatePicker.SelectedDate = DateTime.Now;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                    SignatureСomboBox.SelectedIndex == -1 ||
                    !RegistrationDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Поля не должны быть пустыми. Изменения отменены.");
                    return;
                }

                using (var context = new ArchiveBaseEntities())
                {
                    if (DocumentComboBox.SelectedItem is Document selectedDoc)
                    {
                        var doc = context.Document.Find(selectedDoc.Id);
                        if (doc != null)
                            doc.Title = TitleTextBox.Text;

                        var regCard = context.Registration_Card.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id);
                        if (regCard != null)
                        {
                            regCard.Signature = (bool)SignatureСomboBox.SelectedValue;
                            regCard.Registration_Date = RegistrationDatePicker.SelectedDate.Value;
                            if (currentUserRole != "Администратор")
                                regCard.User_Id = currentUserId;
                        }
                        else
                        {
                            regCard = new Registration_Card
                            {
                                Document_Id = selectedDoc.Id,
                                User_Id = currentUserId,
                                Signature = (bool)SignatureСomboBox.SelectedValue,
                                Registration_Date = RegistrationDatePicker.SelectedDate.Value
                            };
                            context.Registration_Card.Add(regCard);
                        }
                        AuditService.Log(selectedRegCard == null ? "Создана регистрационная карта" : "Обновлена регистрационная карта",
                        "Registration_Card", $"Документ: {selectedDoc.Title}");
                        context.SaveChanges();
                    }
                }

                MessageBox.Show("Изменения сохранены.");
                isEditMode = false;
                EditBtn.Content = "Изменить";
                TitleTextBox.IsReadOnly = true;
                SignatureСomboBox.IsEnabled = false;
                RegistrationDatePicker.IsEnabled = false;

                LoadDocuments();
                LoadRegistrationCards();
                DocumentComboBox_SelectionChanged(DocumentComboBox, null);
            }
        }

        private void DocumentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentComboBox.SelectedItem is Document selectedDocument)
            {
                TitleTextBox.Text = selectedDocument.Title;
                selectedRegCard = RegCards.FirstOrDefault(rc => rc.Document_Id == selectedDocument.Id);

                if (selectedRegCard != null)
                {
                    var user = Users.FirstOrDefault(u => u.Id == selectedRegCard.User_Id);
                    SignedByTextBox.Text = user != null ? $"{user.Last_Name} {user.Name} {user.First_Name}" : "Неизвестно";
                    SignatureСomboBox.SelectedValue = selectedRegCard.Signature;
                    RegistrationDatePicker.SelectedDate = selectedRegCard.Registration_Date;

                    bool isSigned = selectedRegCard.Signature == true;

                    if (currentUserRole == "Администратор")
                    {
                        EditBtn.IsEnabled = true;
                        EditBtn.Content = "Изменить";
                    }
                    else
                    {
                        EditBtn.IsEnabled = !isSigned;
                        EditBtn.Content = isSigned ? "Подписано" : "Изменить";
                    }

                    TitleTextBox.IsReadOnly = true;
                    SignatureСomboBox.IsEnabled = false;
                    RegistrationDatePicker.IsEnabled = false;
                }
                else
                {
                    SignedByTextBox.Text = "";
                    SignatureСomboBox.SelectedIndex = -1;
                    RegistrationDatePicker.SelectedDate = null;
                    EditBtn.IsEnabled = true;
                    EditBtn.Content = "Изменить";
                    TitleTextBox.IsReadOnly = true;
                    SignatureСomboBox.IsEnabled = false;
                    RegistrationDatePicker.IsEnabled = false;
                }
            }
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

            if (isEmptySpace && Keyboard.FocusedElement == TitleTextBox || Keyboard.FocusedElement == RegistrationDatePicker)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement == TitleTextBox || Keyboard.FocusedElement == RegistrationDatePicker)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true;
                }
            }
        }
    }
}