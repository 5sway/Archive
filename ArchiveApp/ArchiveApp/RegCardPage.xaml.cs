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
        private bool isEditMode = false;            // Флаг режима редактирования
        private Registration_Card selectedRegCard = null; // Текущая выбранная карточка регистрации
        public List<KeyValuePair<bool?, string>> StatusList { get; set; } // Список статусов подписи
        private int currentUserId = UserData.CurrentUserId; // ID текущего пользователя
        public List<Document> Documents { get; set; } // Список документов
        public List<User> Users { get; set; }       // Список пользователей
        private string currentUserRole = UserData.CurrentUserRole; // Роль текущего пользователя
        public List<Registration_Card> RegCards { get; set; } // Список карточек регистрации

        public RegCardPage()
        {
            InitializeComponent();                  // Инициализация компонентов страницы
            LoadStatusList();                      // Загрузка списка статусов подписи
            LoadUsers();                           // Загрузка списка пользователей
            LoadRegistrationCards();               // Загрузка карточек регистрации
            LoadDocuments();                       // Загрузка списка документов
        }

        private void LoadStatusList()
        {
            StatusList = new List<KeyValuePair<bool?, string>> // Создание списка статусов
            {
                new KeyValuePair<bool?, string>(true, "Подписан"), // Статус "Подписан"
                new KeyValuePair<bool?, string>(false, "Не подписан") // Статус "Не подписан"
            };
            SignatureСomboBox.ItemsSource = StatusList; // Установка источника данных для ComboBox
            SignatureСomboBox.DisplayMemberPath = "Value"; // Отображаемое поле - текст статуса
            SignatureСomboBox.SelectedValuePath = "Key"; // Значение поля - булево значение
        }

        private void LoadDocuments()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                Documents = context.Document.ToList(); // Загрузка всех документов
                DocumentComboBox.ItemsSource = Documents; // Установка документов в ComboBox
                DocumentComboBox.DisplayMemberPath = "Title"; // Отображение названий документов
                DocumentComboBox.SelectedValuePath = "Id"; // Значение - ID документа
            }
            DocumentComboBox.SelectionChanged += DocumentComboBox_SelectionChanged; // Подписка на изменение выбора
            if (Documents != null && Documents.Any()) // Установка первого документа, если список не пуст
            {
                DocumentComboBox.SelectedItem = Documents.First();
                DocumentComboBox_SelectionChanged(DocumentComboBox, null); // Обновление UI
            }
        }

        private void LoadUsers()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                Users = context.User.ToList();     // Загрузка всех пользователей
            }
        }

        private void LoadRegistrationCards()
        {
            using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
            {
                RegCards = context.Registration_Card // Загрузка карточек с данными
                    .Include("User")                // Включение данных пользователей
                    .Include("Document")            // Включение данных документов
                    .ToList();
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isEditMode)                        // Переключение в режим редактирования
            {
                if (currentUserRole != "Администратор") // Проверка прав для не-администраторов
                {
                    if (selectedRegCard != null && selectedRegCard.Signature == true) // Блокировка редактирования подписанных документов
                    {
                        MessageBox.Show("Документ уже подписан и не может быть изменен");
                        return;
                    }
                }

                isEditMode = true;                  // Включение режима редактирования
                EditBtn.Content = "Сохранить";      // Изменение текста кнопки
                TitleTextBox.IsReadOnly = false;    // Разрешение редактирования названия
                SignatureСomboBox.IsEnabled = true; // Разрешение выбора статуса
                RegistrationDatePicker.IsEnabled = true; // Разрешение выбора даты

                if (currentUserRole != "Администратор") // Установка имени для не-администраторов
                {
                    SignedByTextBox.Text = Users.FirstOrDefault(u => u.Id == currentUserId)?.Name ?? "Неизвестно";
                }

                if (selectedRegCard == null || selectedRegCard.Signature == false || currentUserRole == "Администратор") // Установка текущей даты
                {
                    RegistrationDatePicker.SelectedDate = DateTime.Now;
                }
            }
            else                                    // Сохранение изменений
            {
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || // Проверка заполнения полей
                    SignatureСomboBox.SelectedIndex == -1 ||
                    !RegistrationDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Поля не должны быть пустыми. Изменения отменены.");
                    return;
                }

                using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                {
                    if (DocumentComboBox.SelectedItem is Document selectedDoc) // Обработка выбранного документа
                    {
                        var doc = context.Document.Find(selectedDoc.Id); // Поиск документа в базе
                        if (doc != null)
                            doc.Title = TitleTextBox.Text; // Обновление названия документа

                        var regCard = context.Registration_Card.FirstOrDefault(rc => rc.Document_Id == selectedDoc.Id); // Поиск карточки
                        if (regCard != null)            // Обновление существующей карточки
                        {
                            regCard.Signature = (bool)SignatureСomboBox.SelectedValue; // Обновление статуса подписи
                            regCard.Registration_Date = RegistrationDatePicker.SelectedDate.Value; // Обновление даты
                            if (currentUserRole != "Администратор") // Обновление ID пользователя для не-администраторов
                                regCard.User_Id = currentUserId;
                        }
                        else                            // Создание новой карточки
                        {
                            regCard = new Registration_Card
                            {
                                Document_Id = selectedDoc.Id,
                                User_Id = currentUserId,
                                Signature = (bool)SignatureСomboBox.SelectedValue,
                                Registration_Date = RegistrationDatePicker.SelectedDate.Value
                            };
                            context.Registration_Card.Add(regCard); // Добавление карточки в базу
                        }
                        context.SaveChanges();          // Сохранение изменений в базе
                    }
                }

                MessageBox.Show("Изменения сохранены."); // Уведомление о сохранении
                isEditMode = false;                 // Выход из режима редактирования
                EditBtn.Content = "Изменить";       // Восстановление текста кнопки
                TitleTextBox.IsReadOnly = true;     // Блокировка редактирования названия
                SignatureСomboBox.IsEnabled = false;// Блокировка выбора статуса
                RegistrationDatePicker.IsEnabled = false; // Блокировка выбора даты

                LoadDocuments();                    // Перезагрузка документов
                LoadRegistrationCards();            // Перезагрузка карточек
                DocumentComboBox_SelectionChanged(DocumentComboBox, null); // Обновление UI
            }
        }

        private void DocumentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentComboBox.SelectedItem is Document selectedDocument) // Обработка выбранного документа
            {
                TitleTextBox.Text = selectedDocument.Title; // Отображение названия документа
                selectedRegCard = RegCards.FirstOrDefault(rc => rc.Document_Id == selectedDocument.Id); // Поиск карточки

                if (selectedRegCard != null)        // Если карточка существует
                {
                    var user = Users.FirstOrDefault(u => u.Id == selectedRegCard.User_Id); // Поиск пользователя карточки
                    SignedByTextBox.Text = user?.Name ?? "Неизвестно"; // Отображение имени подписавшего
                    SignatureСomboBox.SelectedValue = selectedRegCard.Signature; // Установка статуса подписи
                    RegistrationDatePicker.SelectedDate = selectedRegCard.Registration_Date; // Установка даты

                    bool isSigned = selectedRegCard.Signature == true; // Проверка статуса подписи

                    if (currentUserRole == "Администратор") // Настройка для администратора
                    {
                        EditBtn.IsEnabled = true;       // Всегда доступно редактирование
                        EditBtn.Content = "Изменить";   // Текст кнопки
                    }
                    else                            // Настройка для не-администраторов
                    {
                        EditBtn.IsEnabled = !isSigned;  // Доступно только для неподписанных
                        EditBtn.Content = isSigned ? "Подписано" : "Изменить"; // Текст кнопки
                    }

                    TitleTextBox.IsReadOnly = true;     // Блокировка редактирования названия
                    SignatureСomboBox.IsEnabled = false;// Блокировка выбора статуса
                    RegistrationDatePicker.IsEnabled = false; // Блокировка выбора даты
                }
                else                                // Если карточка не найдена
                {
                    SignedByTextBox.Text = "";      // Очистка имени
                    SignatureСomboBox.SelectedIndex = -1; // Сброс статуса
                    RegistrationDatePicker.SelectedDate = null; // Сброс даты
                    EditBtn.IsEnabled = true;       // Разрешение создания новой карточки
                    EditBtn.Content = "Изменить";   // Текст кнопки
                    TitleTextBox.IsReadOnly = true; // Блокировка редактирования названия
                    SignatureСomboBox.IsEnabled = false; // Блокировка выбора статуса
                    RegistrationDatePicker.IsEnabled = false; // Блокировка выбора даты
                }
            }
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем элемент, на который был произведён клик
            var clickedElement = e.OriginalSource as DependencyObject;

            // Проверяем, является ли клик по корневому Grid (RootGrid)
            bool isEmptySpace = false;
            while (clickedElement != null)
            {
                if (clickedElement is Grid grid && grid.Name == "MainGrid")
                {
                    isEmptySpace = true;
                    break;
                }
                // Игнорируем клики по интерактивным элементам
                if (clickedElement is Button || clickedElement is TextBox ||
                    clickedElement is TextBlock || clickedElement is Image ||
                    clickedElement is DataGrid || clickedElement is ComboBox)
                {
                    break;
                }
                clickedElement = VisualTreeHelper.GetParent(clickedElement);
            }

            // Если клик был на пустом месте и DocSearchBox в фокусе, снимаем фокус
            if (isEmptySpace && Keyboard.FocusedElement == TitleTextBox || Keyboard.FocusedElement == RegistrationDatePicker)
            {
                Keyboard.ClearFocus();
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Esc или Enter
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // Если один из DatePicker в фокусе, снимаем фокус
                if (Keyboard.FocusedElement == TitleTextBox || Keyboard.FocusedElement == RegistrationDatePicker)
                {
                    Keyboard.ClearFocus();
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
            }
        }
    }
}