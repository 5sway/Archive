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
            // Основной метод экспорта данных в Word документ
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    // Получаем все необходимые данные из базы данных
                    var data = new
                    {
                        Documents = context.Document.ToList(),
                        Requests = context.Request.Include("User").Include("Document").ToList(),
                        Users = context.User.Include("Role").ToList(),
                        RegistrationCards = context.Registration_Card.Include("User").Include("Document").ToList()
                    };

                    // Удаляем существующий файл, если он есть
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // Создаем новый Word документ
                    Word.Application wordApp = new Word.Application();
                    wordApp.Visible = false;
                    Word.Document doc = wordApp.Documents.Add();

                    // Настраиваем стили документа
                    SetDocumentStyles(doc);

                    // Добавляем заголовок и таблицы с данными
                    AddTitle(doc, "Полный отчет архива документов");
                    ExportDocumentsToWord(doc, data.Documents);
                    AddPageBreak(doc);
                    ExportRequestsToWord(doc, data.Requests);
                    AddPageBreak(doc);
                    ExportUsersToWord(doc, data.Users);
                    AddPageBreak(doc);
                    ExportRegistrationCardsToWord(doc, data.RegistrationCards);

                    // Сохраняем и закрываем документ
                    doc.SaveAs2(filePath);
                    doc.Close();
                    wordApp.Quit();

                    // Освобождаем ресурсы
                    ReleaseWordObjects(doc, wordApp);

                    // Открываем экспортированный файл
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void SetDocumentStyles(Word.Document doc)
        {
            doc.Content.Font.Name = "Times New Roman";
            doc.Content.Font.Size = 14;
            doc.Content.ParagraphFormat.LineSpacing = 18f;
            doc.Content.ParagraphFormat.SpaceBefore = 0;
            doc.Content.ParagraphFormat.SpaceAfter = 0;
            doc.Content.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private static void AddTitle(Word.Document doc, string text)
        {
            Word.Paragraph title = doc.Paragraphs.Add();
            title.Range.Text = text;
            title.Range.Font.Bold = 1;
            title.Range.Font.Size = 16;
            title.Format.SpaceBefore = 0;
            title.Format.SpaceAfter = 0;
            title.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            title.Range.InsertParagraphAfter();
        }

        private static void AddPageBreak(Word.Document doc)
        {
            Word.Paragraph lastParagraph = doc.Paragraphs.Add();
            lastParagraph.Range.InsertBreak(Word.WdBreakType.wdPageBreak);
        }

        private static void ExportDocumentsToWord(Word.Document doc, List<Document> documents)
        {
            // Экспорт списка документов в таблицу Word
            AddTableTitle(doc, "Документы");

            // Создаем таблицу с нужными колонками
            Word.Table table = CreateWordTable(doc, new string[] {
        "ID", "Номер", "Название", "Источник", "Копии", "Тип хранения"
    });

            // Заполняем таблицу данными
            foreach (var item in documents)
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

            // Применяем финальное форматирование таблицы
            FinalizeWordTable(table);
        }

        private static void ExportRequestsToWord(Word.Document doc, List<Request> requests)
        {
            AddTableTitle(doc, "Запросы");

            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата", "Причина", "Статус", "Запросил", "Документ" });

            foreach (var item in requests)
            {
                string status = item.Status == true ? "Подтвержден" : "Отклонен";

                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Request_Date.ToShortDateString(),
                    item.Reason,
                    status,
                    item.User?.Name ?? "Неизвестно",
                    item.Document?.Title ?? "Неизвестно"
                });
            }

            FinalizeWordTable(table);
        }

        private static void ExportUsersToWord(Word.Document doc, List<User> users)
        {
            AddTableTitle(doc, "Пользователи");

            Word.Table table = CreateWordTable(doc, new string[] {
                "ID",
                "Логин",
                "Имя",
                "Фамилия",
                "Отчество",
                "Роль",
                "Email",
                "Телефон"
            });

            foreach (var item in users)
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

            FinalizeWordTable(table);
        }

        private static void ExportRegistrationCardsToWord(Word.Document doc, List<Registration_Card> cards)
        {
            AddTableTitle(doc, "Регистрационные карты");

            Word.Table table = CreateWordTable(doc, new string[] {
                "ID",
                "Дата регистрации",
                "Подпись",
                "Кем подписан",
                "Документ"
            });

            foreach (var item in cards)
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Registration_Date.ToShortDateString(),
                    item.Signature ? "Подписан" : "Не подписан",
                    item.User?.Full_Name ?? "Неизвестно",
                    item.Document?.Title ?? "Неизвестно"
                });
            }

            FinalizeWordTable(table);
        }

        private static void AddTableTitle(Word.Document doc, string title)
        {
            Word.Paragraph tableTitle = doc.Paragraphs.Add();
            tableTitle.Range.Text = title;
            tableTitle.Range.Font.Bold = 1;
            tableTitle.Range.Font.Size = 14;
            tableTitle.Format.SpaceBefore = 0;
            tableTitle.Format.SpaceAfter = 0;
            tableTitle.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            tableTitle.Range.InsertParagraphAfter();
        }

        private static Word.Table CreateWordTable(Word.Document doc, string[] headers)
        {
            // Создание таблицы в Word документе
            Word.Table table = doc.Tables.Add(
                doc.Range(doc.Content.End - 1),
                1,  // Начальное количество строк (только для заголовков)
                headers.Length);  // Количество колонок

            // Заполнение заголовков таблицы
            for (int i = 0; i < headers.Length; i++)
            {
                table.Cell(1, i + 1).Range.Text = headers[i];
                table.Cell(1, i + 1).Range.Font.Bold = 1;  // Жирный шрифт для заголовков
                table.Cell(1, i + 1).Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphCenter;  // Выравнивание по центру
            }

            return table;
        }

        private static void AddRowToWordTable(Word.Table table, string[] values)
        {
            // Добавление строки в таблицу Word
            table.Rows.Add();  // Добавляем новую строку
            int rowIndex = table.Rows.Count;  // Получаем индекс новой строки

            // Заполняем ячейки строки данными
            for (int i = 0; i < values.Length; i++)
            {
                table.Cell(rowIndex, i + 1).Range.Text = values[i] ?? "";  // Защита от null
                table.Cell(rowIndex, i + 1).Range.Font.Bold = 0;  // Обычный шрифт
                table.Cell(rowIndex, i + 1).Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphCenter;  // Выравнивание по центру
            }
        }

        private static void FinalizeWordTable(Word.Table table)
        {
            // Финальное форматирование таблицы
            table.Columns.AutoFit();  // Автоподбор ширины колонок
            table.Borders.Enable = 1;  // Включаем границы таблицы

            // Настройка вертикального выравнивания для всех ячеек
            foreach (Word.Row row in table.Rows)
            {
                foreach (Word.Cell cell in row.Cells)
                {
                    cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                }
            }
        }

        private static void ReleaseWordObjects(params object[] objects)
        {
            // Метод для освобождения COM-объектов Word
            foreach (var obj in objects)
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
                catch { }  // Игнорируем ошибки
                finally
                {
                    GC.Collect();  // Принудительный сбор мусора
                }
            }
        }
    }
}