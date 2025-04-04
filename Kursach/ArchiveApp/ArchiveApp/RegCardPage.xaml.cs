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
    /// Логика взаимодействия для RegCardPage.xaml
    /// </summary>
    public partial class RegCardPage : Page
    {
        private bool isEditing = false;
        private bool isEditMode = false;
        private string previousTitle;
        private bool? previousSignature;
        private DateTime? previousDate;
        private Registration_Card selectedRegCard = null;
        public List<KeyValuePair<bool?, string>> StatusList { get; set; }
        private int currentUserId = UserData.CurrentUserId;
        public List<Document> Documents { get; set; }
        public List<User> Users { get; set; }
        public List<Registration_Card> RegCards { get; set; }

        public RegCardPage()
        {
            InitializeComponent();

            LoadStatusList();
            LoadUsers();
            LoadDocuments();
            LoadRegistrationCards();
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>>
            {
                new KeyValuePair<bool?, string>(true, "Подписан"),
                new KeyValuePair<bool?, string>(false, "Не подписан"),
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

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)
            {
                isEditMode = true;

                if (DocumentComboBox.SelectedItem is Document selectedDoc)
                {
                    TitleTextBox.IsReadOnly = false;
                    SignatureСomboBox.IsEnabled = true;
                    RegistrationDatePicker.IsEnabled = true;

                    var regCard = RegCards.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id);
                    previousTitle = TitleTextBox.Text;
                    previousSignature = regCard?.Signature;
                    previousDate = regCard?.Registration_Date;

                    EditBtn.Content = "Сохранить";
                    var currentUser = Users.FirstOrDefault(u => u.Id == currentUserId);
                    if (currentUser != null)
                    {
                        SignedByTextBox.Text = currentUser?.Name ?? "Неизвестно";
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                    SignatureСomboBox.SelectedIndex == -1 ||
                    !RegistrationDatePicker.SelectedDate.HasValue)
                {
                    TitleTextBox.Text = previousTitle;
                    SignatureСomboBox.SelectedValue = previousSignature;
                    RegistrationDatePicker.SelectedDate = previousDate;
                    MessageBox.Show("Поля не должны быть пустыми. Изменения отменены.");
                    return;
                }

                using (var context = new ArchiveBaseEntities())
                {
                    if (DocumentComboBox.SelectedItem is Document selectedDoc)
                    {
                        // Обновляем или добавляем документ
                        var doc = context.Document.FirstOrDefault(d => d.Id == selectedDoc.Id);
                        if (doc != null)
                        {
                            doc.Title = TitleTextBox.Text;
                        }
                        var regCard = context.Registration_Card.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id);
                        if (regCard != null)
                        {
                            regCard.Signature = (bool)SignatureСomboBox.SelectedValue;
                            regCard.Registration_Date = RegistrationDatePicker.SelectedDate.Value.Date;
                        }
                        else
                        {
                            regCard = new Registration_Card
                            {
                                Document_Id = selectedDoc.Id,
                                User_Id = currentUserId,
                                Signature = (bool)SignatureСomboBox.SelectedValue,
                                Registration_Date = RegistrationDatePicker.SelectedDate.Value.Date
                            };

                            context.Registration_Card.Add(regCard);
                        }

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
                    if (user != null)
                    {
                        SignedByTextBox.Text = user?.Name ?? "Неизвестно";
                    }

                    SignatureСomboBox.SelectedValue = selectedRegCard.Signature;
                    RegistrationDatePicker.SelectedDate = selectedRegCard.Registration_Date;
                }
                else
                {
                    SignedByTextBox.Text = "";
                    SignatureСomboBox.SelectedIndex = -1;
                    RegistrationDatePicker.SelectedDate = null;
                }

                isEditing = false;
                EditBtn.Content = "Изменить";
                TitleTextBox.IsReadOnly = true;
                SignatureСomboBox.IsEnabled = false;
            }
        }
    }
}


