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
            LoadRegistrationCards();
            LoadDocuments();
            
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
            // Загрузка списка документов из базы данных
            using (var context = new ArchiveBaseEntities())
            {
                Documents = context.Document.ToList();
                // Настройка ComboBox для выбора документов
                DocumentComboBox.ItemsSource = Documents;
                DocumentComboBox.DisplayMemberPath = "Title"; // Отображаемое поле
                DocumentComboBox.SelectedValuePath = "Id";    // Значение поля
            }

            // Подписка на событие изменения выбранного документа
            DocumentComboBox.SelectionChanged += DocumentComboBox_SelectionChanged;

            // Установка первого документа по умолчанию, если список не пуст
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
            // Загрузка карточек регистрации с включением связанных данных
            using (var context = new ArchiveBaseEntities())
            {
                RegCards = context.Registration_Card
                                  .Include("User")     // Загрузка данных пользователя
                                  .Include("Document") // Загрузка данных документа
                                  .ToList();
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            // Переключение между режимами редактирования и сохранения
            if (!isEditMode)
            {
                // Вход в режим редактирования
                isEditMode = true;

                if (DocumentComboBox.SelectedItem is Document selectedDoc)
                {
                    // Активация полей для редактирования
                    TitleTextBox.IsReadOnly = false;
                    SignatureСomboBox.IsEnabled = true;
                    RegistrationDatePicker.IsEnabled = true;

                    // Сохранение предыдущих значений для возможного отката
                    var regCard = RegCards.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id);
                    previousTitle = TitleTextBox.Text;
                    previousSignature = regCard?.Signature;
                    previousDate = regCard?.Registration_Date;

                    // Настройка интерфейса
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
                // Проверка заполнения обязательных полей
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text) ||
                    SignatureСomboBox.SelectedIndex == -1 ||
                    !RegistrationDatePicker.SelectedDate.HasValue)
                {
                    // Откат изменений при невалидных данных
                    TitleTextBox.Text = previousTitle;
                    SignatureСomboBox.SelectedValue = previousSignature;
                    RegistrationDatePicker.SelectedDate = previousDate;
                    MessageBox.Show("Поля не должны быть пустыми. Изменения отменены.");
                    return;
                }

                // Сохранение изменений в базе данных
                using (var context = new ArchiveBaseEntities())
                {
                    if (DocumentComboBox.SelectedItem is Document selectedDoc)
                    {
                        // Обновление данных документа
                        var doc = context.Document.FirstOrDefault(d => d.Id == selectedDoc.Id);
                        if (doc != null)
                        {
                            doc.Title = TitleTextBox.Text;
                        }

                        // Обновление или создание карточки регистрации
                        var regCard = context.Registration_Card.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id);
                        if (regCard != null)
                        {
                            regCard.Signature = (bool)SignatureСomboBox.SelectedValue;
                            regCard.Registration_Date = RegistrationDatePicker.SelectedDate.Value.Date;
                        }
                        else
                        {
                            // Создание новой карточки регистрации
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
                // Выход из режима редактирования
                isEditMode = false;
                EditBtn.Content = "Изменить";
                TitleTextBox.IsReadOnly = true;
                SignatureСomboBox.IsEnabled = false;
                RegistrationDatePicker.IsEnabled = false;

                // Перезагрузка данных
                LoadDocuments();
                LoadRegistrationCards();
            }
        }

        private void DocumentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка изменения выбранного документа
            if (DocumentComboBox.SelectedItem is Document selectedDocument)
            {
                // Отображение названия документа
                TitleTextBox.Text = selectedDocument.Title;

                if (RegCards != null)
                {
                    // Поиск карточки регистрации для выбранного документа
                    selectedRegCard = RegCards.FirstOrDefault(rc => rc.Document_Id == selectedDocument.Id);

                    if (selectedRegCard != null)
                    {
                        // Если карточка найдена, заполняем данные
                        var user = Users.FirstOrDefault(u => u.Id == selectedRegCard.User_Id);
                        SignedByTextBox.Text = user?.Name ?? "Неизвестно";
                        SignatureСomboBox.SelectedValue = selectedRegCard.Signature;
                        RegistrationDatePicker.SelectedDate = selectedRegCard.Registration_Date;
                    }
                    else
                    {
                        // Если карточка не найдена, очищаем поля
                        SignedByTextBox.Text = "";
                        SignatureСomboBox.SelectedIndex = -1;
                        RegistrationDatePicker.SelectedDate = null;
                    }
                }

                // Сброс режима редактирования
                EditBtn.Content = "Изменить";
                TitleTextBox.IsReadOnly = true;
                SignatureСomboBox.IsEnabled = false;
            }
        }
    }
}


