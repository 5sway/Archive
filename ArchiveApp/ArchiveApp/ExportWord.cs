using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Word = Microsoft.Office.Interop.Word;

namespace ArchiveApp
{
    class ExportWord
    {
        public static void ExportToWord(string filePath)
        {
            try
            {
                using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                {
                    var data = new                     // Загрузка всех данных из базы
                    {
                        Documents = context.Document.ToList(),
                        Requests = context.Request.Include("User").Include("Document").ToList(),
                        Users = context.User.Include("Role").ToList(),
                        RegistrationCards = context.Registration_Card.Include("User").Include("Document").ToList()
                    };

                    if (File.Exists(filePath)) File.Delete(filePath); // Удаление существующего файла

                    Word.Application wordApp = new Word.Application(); // Создание приложения Word
                    wordApp.Visible = false;           // Скрытие интерфейса Word
                    Word.Document doc = wordApp.Documents.Add(); // Создание нового документа

                    SetDocumentStyles(doc);            // Настройка стилей документа
                    AddTitle(doc, "Полный отчет архива документов"); // Добавление заголовка

                    ExportDocumentsToWord(doc, data.Documents); // Экспорт документов

                    if (data.Requests.Any() || data.Users.Any() || data.RegistrationCards.Any()) // Условный разрыв страницы
                        AddPageBreak(doc);

                    if (data.Requests.Any())           // Экспорт запросов
                    {
                        ExportRequestsToWord(doc, data.Requests);
                        if (data.Users.Any() || data.RegistrationCards.Any()) AddPageBreak(doc);
                    }

                    if (data.Users.Any())              // Экспорт пользователей
                    {
                        ExportUsersToWord(doc, data.Users);
                        if (data.RegistrationCards.Any()) AddPageBreak(doc);
                    }

                    if (data.RegistrationCards.Any())  // Экспорт регистрационных карточек
                        ExportRegistrationCardsToWord(doc, data.RegistrationCards);

                    doc.SaveAs2(filePath);             // Сохранение документа
                    doc.Close();                       // Закрытие документа
                    wordApp.Quit();                    // Закрытие приложения Word

                    ReleaseWordObjects(doc, wordApp);  // Освобождение ресурсов
                    OpenExportedFile(filePath);        // Открытие файла
                }
            }
            catch (Exception ex)                        // Обработка ошибок
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }); // Открытие файла через оболочку
            }
            catch (Exception ex)                    // Обработка ошибок открытия
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void SetDocumentStyles(Word.Document doc)
        {
            doc.Content.Font.Name = "Times New Roman"; // Установка шрифта
            doc.Content.Font.Size = 14;             // Установка размера шрифта
            doc.Content.ParagraphFormat.LineSpacing = 18f; // Межстрочный интервал
            doc.Content.ParagraphFormat.SpaceBefore = 0; // Отступ перед абзацем
            doc.Content.ParagraphFormat.SpaceAfter = 0; // Отступ после абзаца
            doc.Content.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Выравнивание по центру
        }

        private static void AddTitle(Word.Document doc, string text)
        {
            Word.Paragraph title = doc.Paragraphs.Add(); // Добавление абзаца для заголовка
            title.Range.Text = text;                // Установка текста заголовка
            title.Range.Font.Bold = 1;              // Жирный шрифт
            title.Range.Font.Size = 16;             // Размер шрифта заголовка
            title.Format.SpaceBefore = 0;           // Отступ перед заголовком
            title.Format.SpaceAfter = 0;            // Отступ после заголовка
            title.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Выравнивание по центру
            title.Range.InsertParagraphAfter();     // Добавление пустого абзаца после
        }

        private static void AddPageBreak(Word.Document doc)
        {
            Word.Paragraph lastParagraph = doc.Paragraphs.Add(); // Добавление абзаца для разрыва
            lastParagraph.Range.InsertBreak(Word.WdBreakType.wdPageBreak); // Вставка разрыва страницы
        }

        private static void ExportDocumentsToWord(Word.Document doc, List<Document> documents)
        {
            AddTableTitle(doc, "Документы");        // Добавление заголовка таблицы
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Номер", "Название", "Источник", "Копии", "Тип хранения" }); // Создание таблицы

            foreach (var item in documents)         // Заполнение таблицы данными документов
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Number,
                    item.Title,
                    item.Source,
                    item.Copies_Count.ToString(),
                    item.Storage_Type
                });
            }
            FinalizeWordTable(table);               // Финальное форматирование таблицы
        }

        private static void ExportRequestsToWord(Word.Document doc, List<Request> requests)
        {
            AddTableTitle(doc, "Запросы");          // Добавление заголовка таблицы
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата", "Причина", "Статус", "Запросил", "Документ" }); // Создание таблицы

            foreach (var item in requests)          // Заполнение таблицы данными запросов
            {
                string status = item.Status == true ? "Подтвержден" : "Отклонен"; // Форматирование статуса
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Request_Date.ToShortDateString(),
                    item.Reason,
                    status,
                    item.User?.Name ?? "Неизвестно",
                    item.Document?.Title ?? "Неизвестно"
                });
            }
            FinalizeWordTable(table);               // Финальное форматирование таблицы
        }

        private static void ExportUsersToWord(Word.Document doc, List<User> users)
        {
            AddTableTitle(doc, "Пользователи");     // Добавление заголовка таблицы
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Логин", "Имя", "Фамилия", "Отчество", "Роль", "Email", "Телефон" }); // Создание таблицы

            foreach (var item in users)             // Заполнение таблицы данными пользователей
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Login,
                    item.Name,
                    item.Full_Name,
                    item.First_Name,
                    item.Role?.Name ?? "Неизвестно",
                    item.Email,
                    item.Phone_Number
                });
            }
            FinalizeWordTable(table);               // Финальное форматирование таблицы
        }

        private static void ExportRegistrationCardsToWord(Word.Document doc, List<Registration_Card> cards)
        {
            AddTableTitle(doc, "Регистрационные карты"); // Добавление заголовка таблицы
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата регистрации", "Подпись", "Кем подписан", "Документ" }); // Создание таблицы

            foreach (var item in cards)             // Заполнение таблицы данными карточек
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Registration_Date.ToShortDateString(),
                    item.Signature ? "Подписан" : "Не подписан",
                    item.User?.Full_Name ?? "Неизвестно",
                    item.Document?.Title ?? "Неизвестно"
                });
            }
            FinalizeWordTable(table);               // Финальное форматирование таблицы
        }

        private static void AddTableTitle(Word.Document doc, string title)
        {
            Word.Paragraph tableTitle = doc.Paragraphs.Add(); // Добавление абзаца для заголовка таблицы
            tableTitle.Range.Text = title;          // Установка текста заголовка
            tableTitle.Range.Font.Bold = 1;         // Жирный шрифт
            tableTitle.Range.Font.Size = 14;        // Размер шрифта
            tableTitle.Format.SpaceBefore = 0;      // Отступ перед заголовком
            tableTitle.Format.SpaceAfter = 0;       // Отступ после заголовка
            tableTitle.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Выравнивание по центру
            tableTitle.Range.InsertParagraphAfter(); // Добавление пустого абзаца после
        }

        private static Word.Table CreateWordTable(Word.Document doc, string[] headers)
        {
            Word.Table table = doc.Tables.Add(doc.Range(doc.Content.End - 1), 1, headers.Length); // Создание таблицы с одной строкой
            for (int i = 0; i < headers.Length; i++) // Заполнение заголовков
            {
                table.Cell(1, i + 1).Range.Text = headers[i]; // Установка текста заголовка
                table.Cell(1, i + 1).Range.Font.Bold = 1; // Жирный шрифт для заголовков
                table.Cell(1, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Выравнивание по центру
            }
            return table;                           // Возврат созданной таблицы
        }

        private static void AddRowToWordTable(Word.Table table, string[] values)
        {
            table.Rows.Add();                       // Добавление новой строки
            int rowIndex = table.Rows.Count;        // Индекс новой строки
            for (int i = 0; i < values.Length; i++) // Заполнение ячеек строки
            {
                table.Cell(rowIndex, i + 1).Range.Text = values[i] ?? ""; // Установка текста ячейки
                table.Cell(rowIndex, i + 1).Range.Font.Bold = 0; // Обычный шрифт
                table.Cell(rowIndex, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter; // Выравнивание по центру
            }
        }

        private static void FinalizeWordTable(Word.Table table)
        {
            table.Columns.AutoFit();                // Автоматическая подгонка ширины колонок
            table.Borders.Enable = 1;               // Включение границ таблицы
            foreach (Word.Row row in table.Rows)    // Настройка выравнивания ячеек
            {
                foreach (Word.Cell cell in row.Cells)
                {
                    cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter; // Вертикальное выравнивание по центру
                }
            }
        }

        private static void ReleaseWordObjects(params object[] objects)
        {
            foreach (var obj in objects)            // Освобождение COM-объектов
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj); // Освобождение объекта
                }
                catch { }                           // Игнорирование ошибок
                finally
                {
                    GC.Collect();                   // Принудительный сбор мусора
                }
            }
        }
    }
}