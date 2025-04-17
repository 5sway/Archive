using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Word = Microsoft.Office.Interop.Word;

namespace ArchiveApp
{
    class ExportWord
    {
        public static void ExportToWord(string filePath, List<string> selectedTables, DateTime? startDate, DateTime? endDate, string userRole, string format)
        {
            Word.Application wordApp = null;
            Word.Document doc = null;
            try
            {
                if (string.IsNullOrEmpty(filePath) || selectedTables == null || string.IsNullOrEmpty(userRole) || string.IsNullOrEmpty(format))
                {
                    MessageBox.Show("Ошибка: некорректные параметры экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var context = new ArchiveBaseEntities())
                {
                    var data = new
                    {
                        Documents = context.Document.ToList() ?? new List<Document>(),
                        Requests = context.Request.Include("User").Include("Document").ToList() ?? new List<Request>(),
                        Users = context.User.Include("Role").ToList() ?? new List<User>(),
                        RegistrationCards = context.Registration_Card.Include("User").Include("Document").ToList() ?? new List<Registration_Card>()
                    };

                    // Фильтрация по периоду, если указан
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        data = new
                        {
                            Documents = data.Documents.Where(d => d.Receipt_Date >= startDate && d.Receipt_Date <= endDate).ToList(),
                            Requests = data.Requests.Where(r => r.Request_Date >= startDate && r.Request_Date <= endDate).ToList(),
                            Users = data.Users, // Пользователи не фильтруются по дате
                            RegistrationCards = data.RegistrationCards.Where(c => c.Registration_Date >= startDate && c.Registration_Date <= endDate).ToList()
                        };
                    }

                    // Учет роли: делопроизводитель не видит запросы
                    if (userRole == "Делопроизводитель")
                    {
                        selectedTables.Remove("Requests");
                    }

                    // Удаляем существующий файл, если он есть
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    wordApp = new Word.Application();
                    wordApp.Visible = false;
                    wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone; // Отключаем предупреждения

                    doc = wordApp.Documents.Add();

                    SetDocumentStyles(doc);
                    AddTitle(doc, startDate.HasValue ? $"Отчет за период {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}" : "Отчет");

                    bool isFirstTable = true;
                    foreach (var table in selectedTables)
                    {
                        if (!isFirstTable && doc.Paragraphs.Count > 1)
                            AddPageBreak(doc);

                        switch (table)
                        {
                            case "Documents":
                                if (data.Documents.Any())
                                    ExportDocumentsToWord(doc, data.Documents);
                                break;
                            case "Requests":
                                if (data.Requests.Any())
                                    ExportRequestsToWord(doc, data.Requests);
                                break;
                            case "Users":
                                if (data.Users.Any())
                                    ExportUsersToWord(doc, data.Users);
                                break;
                            case "RegistrationCards":
                                if (data.RegistrationCards.Any())
                                    ExportRegistrationCardsToWord(doc, data.RegistrationCards);
                                break;
                            default:
                                MessageBox.Show($"Неизвестная таблица: {table}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                break;
                        }
                        isFirstTable = false;
                    }

                    // Определяем формат сохранения
                    Word.WdSaveFormat saveFormat = format.Equals("PDF", StringComparison.OrdinalIgnoreCase)
                        ? Word.WdSaveFormat.wdFormatPDF
                        : Word.WdSaveFormat.wdFormatDocumentDefault;

                    // Сохраняем только один раз
                    doc.SaveAs2(filePath, saveFormat);

                    // Закрываем без сохранения изменений
                    object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref doNotSave);
                    wordApp.Quit();

                    // Освобождаем ресурсы
                    ReleaseWordObjects(doc, wordApp);

                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                // Закрываем при ошибке
                if (doc != null)
                {
                    object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref doNotSave);
                }
                if (wordApp != null) wordApp.Quit();

                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Файл отчета не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            title.Range.Text = text + "\r\n";
            title.Range.Font.Bold = 1;
            title.Range.Font.Size = 16;
            title.Format.SpaceBefore = 0;
            title.Format.SpaceAfter = 0;
            title.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private static void AddPageBreak(Word.Document doc)
        {
            Word.Paragraph lastParagraph = doc.Paragraphs.Add();
            lastParagraph.Range.InsertBreak(Word.WdBreakType.wdPageBreak);
        }

        private static void ExportDocumentsToWord(Word.Document doc, List<Document> documents)
        {
            AddTableTitle(doc, "Документы");
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Номер", "Дата", "Название", "Источник", "Копии", "Тип хранения" });

            foreach (var item in documents)
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Number ?? "",
                    item.Receipt_Date.ToShortDateString() ?? "",
                    item.Title ?? "",
                    item.Source ?? "",
                    item.Copies_Count.ToString(),
                    item.Storage_Type ?? ""
                });
            }
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
                    item.Request_Date.ToShortDateString() ?? "",
                    item.Reason ?? "",
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
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Логин", "Имя", "Фамилия", "Отчество", "Роль", "Email", "Телефон" });

            foreach (var item in users)
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Login ?? "",
                    item.Name ?? "",
                    item.Last_Name ?? "",
                    item.First_Name ?? "",
                    item.Role?.Name ?? "Неизвестно",
                    item.Email ?? "",
                    item.Phone_Number ?? ""
                });
            }
            FinalizeWordTable(table);
        }

        private static void ExportRegistrationCardsToWord(Word.Document doc, List<Registration_Card> cards)
        {
            AddTableTitle(doc, "Регистрационные карты");
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата регистрации", "Подпись", "Кем подписан", "Документ" });

            foreach (var item in cards)
            {
                AddRowToWordTable(table, new string[] {
                    item.Id.ToString(),
                    item.Registration_Date.ToShortDateString(),
                    item.Signature ? "Подписан" : "Не подписан",
                    item.User?.Last_Name ?? "Неизвестно",
                    item.Document?.Title ?? "Неизвестно"
                });
            }
            FinalizeWordTable(table);
        }

        private static void AddTableTitle(Word.Document doc, string title)
        {
            Word.Paragraph tableTitle = doc.Paragraphs.Add();
            tableTitle.Range.Text = title + "\r\n";
            tableTitle.Range.Font.Bold = 1;
            tableTitle.Range.Font.Size = 14;
            tableTitle.Format.SpaceBefore = 0;
            tableTitle.Format.SpaceAfter = 0;
            tableTitle.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private static Word.Table CreateWordTable(Word.Document doc, string[] headers)
        {
            Word.Table table = doc.Tables.Add(doc.Range(doc.Content.End - 1), 1, headers.Length);
            for (int i = 0; i < headers.Length; i++)
            {
                table.Cell(1, i + 1).Range.Text = headers[i];
                table.Cell(1, i + 1).Range.Font.Bold = 1;
                table.Cell(1, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }
            return table;
        }

        private static void AddRowToWordTable(Word.Table table, string[] values)
        {
            table.Rows.Add();
            int rowIndex = table.Rows.Count;
            for (int i = 0; i < values.Length; i++)
            {
                table.Cell(rowIndex, i + 1).Range.Text = values[i] ?? "";
                table.Cell(rowIndex, i + 1).Range.Font.Bold = 0;
                table.Cell(rowIndex, i + 1).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            }
        }

        private static void FinalizeWordTable(Word.Table table)
        {
            table.Columns.AutoFit();
            table.Borders.Enable = 1;
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
            if (objects == null) return;

            foreach (var obj in objects)
            {
                try
                {
                    if (obj != null && System.Runtime.InteropServices.Marshal.IsComObject(obj))
                    {
                        // Освобождаем COM-объект
                        while (System.Runtime.InteropServices.Marshal.ReleaseComObject(obj) > 0)
                        {
                            // Продолжаем Release, пока счетчик ссылок не станет 0
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем ошибку, но не прерываем выполнение
                    Debug.WriteLine($"Ошибка при освобождении COM-объекта: {ex.Message}");
                }
                finally
                {
                    // Для managed объектов просто убеждаемся, что они доступны для GC
                    if (obj != null && !System.Runtime.InteropServices.Marshal.IsComObject(obj))
                    {
                    }
                }
            }

            // Принудительный сбор мусора
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(); // Дополнительный сбор для надежности
        }
    }
}