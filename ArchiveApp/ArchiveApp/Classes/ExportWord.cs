using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Word = Microsoft.Office.Interop.Word;

namespace ArchiveApp
{
    class ExportWord
    {
        public static void ExportToWord(string filePath, List<string> selectedTables, Dictionary<string, List<int>> selectedRecordIds,
            DateTime? startDate, DateTime? endDate, string userRole, string format, bool isTableFormat)
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
                    // Получаем данные составителя
                    var currentUser = context.User.Include("Role").FirstOrDefault(u => u.Role.Name == userRole);
                    string compilerInfo = currentUser != null
                        ? $"{currentUser.Role?.Name ?? "Должность не указана"} {currentUser.Last_Name} {currentUser.Name} {currentUser.First_Name}"
                        : "Составитель не определен";

                    var data = new
                    {
                        Documents = context.Document.ToList() ?? new List<Document>(),
                        Requests = context.Request.Include("User").Include("Document").ToList() ?? new List<Request>(),
                        Users = context.User.Include("Role").ToList() ?? new List<User>(),
                        RegistrationCards = context.Registration_Card.Include("User").Include("Document").ToList() ?? new List<Registration_Card>()
                    };

                    // Фильтрация данных по выбранным записям и периоду
                    if (selectedRecordIds != null && selectedRecordIds.Any())
                    {
                        data = new
                        {
                            Documents = data.Documents.Where(d => selectedRecordIds.ContainsKey("Documents")
                                ? selectedRecordIds["Documents"].Contains(d.Id)
                                : true).ToList(),
                            Requests = data.Requests.Where(r => selectedRecordIds.ContainsKey("Requests")
                                ? selectedRecordIds["Requests"].Contains(r.Id)
                                : true).ToList(),
                            Users = data.Users.Where(u => selectedRecordIds.ContainsKey("Users")
                                ? selectedRecordIds["Users"].Contains(u.Id)
                                : true).ToList(),
                            RegistrationCards = data.RegistrationCards.Where(c => selectedRecordIds.ContainsKey("RegistrationCards")
                                ? selectedRecordIds["RegistrationCards"].Contains(c.Id)
                                : true).ToList()
                        };
                    }

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
                    wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;

                    doc = wordApp.Documents.Add();

                    // Добавляем титульный лист
                    AddTitlePage(doc, compilerInfo, startDate, endDate);

                    // Основное содержимое отчета
                    SetDocumentStyles(doc);
                    AddReportTitle(doc, startDate, endDate);

                    bool isFirstTable = true;
                    foreach (var table in selectedTables)
                    {
                        if (!isFirstTable && doc.Paragraphs.Count > 1)
                            AddPageBreak(doc);

                        switch (table)
                        {
                            case "Documents":
                                if (data.Documents.Any())
                                {
                                    if (isTableFormat)
                                        ExportDocumentsToWordTable(doc, data.Documents);
                                    else
                                        ExportDocumentsToWordText(doc, data.Documents);
                                }
                                break;
                            case "Requests":
                                if (data.Requests.Any())
                                {
                                    if (isTableFormat)
                                        ExportRequestsToWordTable(doc, data.Requests);
                                    else
                                        ExportRequestsToWordText(doc, data.Requests);
                                }
                                break;
                            case "Users":
                                if (data.Users.Any())
                                {
                                    if (isTableFormat)
                                        ExportUsersToWordTable(doc, data.Users);
                                    else
                                        ExportUsersToWordText(doc, data.Users);
                                }
                                break;
                            case "RegistrationCards":
                                if (data.RegistrationCards.Any())
                                {
                                    if (isTableFormat)
                                        ExportRegistrationCardsToWordTable(doc, data.RegistrationCards);
                                    else
                                        ExportRegistrationCardsToWordText(doc, data.RegistrationCards);
                                }
                                break;
                        }
                        isFirstTable = false;
                    }

                    // Сохранение в выбранном формате
                    Word.WdSaveFormat saveFormat = format.Equals("PDF", StringComparison.OrdinalIgnoreCase)
                        ? Word.WdSaveFormat.wdFormatPDF
                        : Word.WdSaveFormat.wdFormatDocumentDefault;

                    doc.SaveAs2(filePath, saveFormat);

                    // Закрытие документа
                    object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref doNotSave);
                    wordApp.Quit();

                    ReleaseWordObjects(doc, wordApp);
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                if (doc != null)
                {
                    object doNotSave = Word.WdSaveOptions.wdDoNotSaveChanges;
                    doc.Close(ref doNotSave);
                }
                if (wordApp != null) wordApp.Quit();

                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            title.Range.Font.Size = 14;
            title.Format.SpaceBefore = 0;
            title.Format.SpaceAfter = 0;
            title.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
        }

        private static void AddPageBreak(Word.Document doc)
        {
            Word.Paragraph lastParagraph = doc.Paragraphs.Add();
            lastParagraph.Range.InsertBreak(Word.WdBreakType.wdPageBreak);
        }


        private static void ExportDocumentsToWordTable(Word.Document doc, List<Document> documents)
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

        private static void AddTableTitle(Word.Document doc, string title)
        {
            Word.Paragraph tableTitle = doc.Paragraphs.Add();
            Word.Range range = tableTitle.Range;
            range.Text = title;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14; // Исправлено с 12 на 14
            range.Font.Bold = 1;
            range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            range.ParagraphFormat.SpaceBefore = 12;
            range.ParagraphFormat.SpaceAfter = 6;
            range.InsertParagraphAfter();
        }

        private static void AddTitlePage(Word.Document doc, string compilerInfo, DateTime? startDate, DateTime? endDate)
        {
            // Верхний заголовок
            Word.Paragraph header = doc.Paragraphs.Add();
            Word.Range headerRange = header.Range;
            headerRange.Text = "МИНИСТЕРСТВО ОБРАЗОВАНИЯ И НАУКИ РЕСПУБЛИКИ КОМИ\n" +
                               "Государственное профессиональное образовательное учреждение\n" +
                               "«Воркутинский арктический горно-политехнический колледж»\n";
            headerRange.Font.Name = "Times New Roman";
            headerRange.Font.Size = 12;
            headerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            headerRange.ParagraphFormat.SpaceAfter = 12;
            headerRange.InsertParagraphAfter();

            // Разделительная линия
            AddSeparator(doc, 24);

            // Название отчета
            Word.Paragraph title = doc.Paragraphs.Add();
            Word.Range titleRange = title.Range;
            titleRange.Text = "ОТЧЕТ\n";
            titleRange.Font.Bold = 1;
            titleRange.Font.Size = 16;
            titleRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            titleRange.ParagraphFormat.SpaceAfter = 12;
            titleRange.InsertParagraphAfter();

            // Период отчета
            if (startDate.HasValue && endDate.HasValue)
            {
                Word.Paragraph period = doc.Paragraphs.Add();
                Word.Range periodRange = period.Range;
                periodRange.Text = $"За период: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}\n";
                periodRange.Font.Size = 14;
                periodRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                periodRange.ParagraphFormat.SpaceAfter = 24;
                periodRange.InsertParagraphAfter();
            }

            // Составитель
            Word.Paragraph compiler = doc.Paragraphs.Add();
            Word.Range compilerRange = compiler.Range;
            compilerRange.Text = $"Составитель: {compilerInfo}\n";
            compilerRange.Font.Size = 14;
            compilerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            compilerRange.ParagraphFormat.SpaceBefore = 24;
            compilerRange.ParagraphFormat.SpaceAfter = 24;
            compilerRange.InsertParagraphAfter();

            // Дата и город
            Word.Paragraph footer = doc.Paragraphs.Add();
            Word.Range footerRange = footer.Range;
            footerRange.Text = $"Воркута, {DateTime.Now:yyyy}";
            footerRange.Font.Size = 14;
            footerRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
            footerRange.InsertParagraphAfter();

            // Разрыв страницы после титульного листа
            AddPageBreak(doc);
        }

        private static void AddReportTitle(Word.Document doc, DateTime? startDate, DateTime? endDate)
        {
            Word.Paragraph title = doc.Paragraphs.Add();
            Word.Range range = title.Range;
            range.Text = startDate.HasValue
                ? $"Отчет за период {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}"
                : "Отчет";
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14;
            range.Font.Bold = 1;
            range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            range.ParagraphFormat.SpaceAfter = 12;
            range.InsertParagraphAfter();
        }
        private static void ExportUsersToWordTable(Word.Document doc, List<User> users)
        {
            AddTableTitle(doc, "Пользователи");
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Логин", "ФИО", "Роль", "Email", "Телефон" });

            foreach (var item in users)
            {
                string fullName = $"{item.Last_Name} {item.Name} {item.First_Name}";
                AddRowToWordTable(table, new string[] {
            item.Id.ToString(),
            item.Login ?? "",
            fullName.Trim(),
            item.Role?.Name ?? "Неизвестно",
            item.Email ?? "",
            item.Phone_Number ?? ""
        });
            }
            FinalizeWordTable(table);
        }

        private static void ExportUsersToWordText(Word.Document doc, List<User> users)
        {
            AddSectionTitle(doc, "Пользователи");

            foreach (var item in users)
            {
                string fullName = $"{item.Last_Name} {item.Name} {item.First_Name}";
                AddTextParagraph(doc, $"ID: {item.Id}", bold: true);
                AddTextParagraph(doc, $"Логин: {item.Login ?? "Не указан"}");
                AddTextParagraph(doc, $"ФИО: {fullName.Trim()}");
                AddTextParagraph(doc, $"Роль: {item.Role?.Name ?? "Неизвестно"}");
                AddTextParagraph(doc, $"Email: {item.Email ?? "Не указан"}");
                AddTextParagraph(doc, $"Телефон: {item.Phone_Number ?? "Не указан"}");
                AddSeparator(doc);
            }
        }

        private static void ExportRequestsToWordTable(Word.Document doc, List<Request> requests)
        {
            AddTableTitle(doc, "Запросы");
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата", "Причина", "Статус", "Запросил (ФИО)", "Документ" });

            foreach (var item in requests)
            {
                string status = item.Status == true ? "Подтвержден" : "Отклонен";
                string requesterName = item.User != null
                    ? $"{item.User.Last_Name} {item.User.Name} {item.User.First_Name}"
                    : "Неизвестно";

                AddRowToWordTable(table, new string[] {
            item.Id.ToString(),
            item.Request_Date.ToShortDateString() ?? "",
            item.Reason ?? "",
            status,
            requesterName,
            item.Document?.Title ?? "Неизвестно"
        });
            }
            FinalizeWordTable(table);
        }

        private static void ExportRequestsToWordText(Word.Document doc, List<Request> requests)
        {
            AddSectionTitle(doc, "Запросы");

            foreach (var item in requests)
            {
                string status = item.Status == true ? "Подтвержден" : "Отклонен";
                string requesterName = item.User != null
                    ? $"{item.User.Last_Name} {item.User.Name} {item.User.First_Name}"
                    : "Неизвестно";

                AddTextParagraph(doc, $"ID: {item.Id}", bold: true);
                AddTextParagraph(doc, $"Дата запроса: {item.Request_Date.ToShortDateString()}");
                AddTextParagraph(doc, $"Причина: {item.Reason ?? "Не указана"}");
                AddTextParagraph(doc, $"Статус: {status}");
                AddTextParagraph(doc, $"Запросил: {requesterName}");
                AddTextParagraph(doc, $"Документ: {item.Document?.Title ?? "Неизвестно"}");
                AddSeparator(doc);
            }
        }

        private static void ExportRegistrationCardsToWordTable(Word.Document doc, List<Registration_Card> cards)
        {
            AddTableTitle(doc, "Регистрационные карты");
            Word.Table table = CreateWordTable(doc, new string[] { "ID", "Дата регистрации", "Подпись", "Кем подписан (ФИО)", "Документ" });

            foreach (var item in cards)
            {
                string signerName = item.User != null
                    ? $"{item.User.Last_Name} {item.User.Name} {item.User.First_Name}"
                    : "Неизвестно";

                AddRowToWordTable(table, new string[] {
            item.Id.ToString(),
            item.Registration_Date.ToShortDateString(),
            item.Signature ? "Подписан" : "Не подписан",
            signerName,
            item.Document?.Title ?? "Неизвестно"
        });
            }
            FinalizeWordTable(table);
        }

        private static void ExportRegistrationCardsToWordText(Word.Document doc, List<Registration_Card> cards)
        {
            AddSectionTitle(doc, "Регистрационные карты");

            foreach (var item in cards)
            {
                string signerName = item.User != null
                    ? $"{item.User.Last_Name} {item.User.Name} {item.User.First_Name}"
                    : "Неизвестно";

                AddTextParagraph(doc, $"ID: {item.Id}", bold: true);
                AddTextParagraph(doc, $"Дата регистрации: {item.Registration_Date.ToShortDateString()}");
                AddTextParagraph(doc, $"Подпись: {(item.Signature ? "Подписан" : "Не подписан")}");
                AddTextParagraph(doc, $"Кем подписан: {signerName}");
                AddTextParagraph(doc, $"Документ: {item.Document?.Title ?? "Неизвестно"}");
                AddSeparator(doc);
            }
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

        private static void ExportDocumentsToWordText(Word.Document doc, List<Document> documents)
        {
            AddSectionTitle(doc, "Документы");

            foreach (var item in documents)
            {
                AddTextParagraph(doc, $"ID: {item.Id}", bold: true);
                AddTextParagraph(doc, $"Номер: {item.Number ?? "Не указан"}");
                AddTextParagraph(doc, $"Дата поступления: {item.Receipt_Date.ToShortDateString()}");
                AddTextParagraph(doc, $"Название: {item.Title ?? "Не указано"}");
                AddTextParagraph(doc, $"Источник: {item.Source ?? "Не указан"}");
                AddTextParagraph(doc, $"Количество копий: {item.Copies_Count}");
                AddTextParagraph(doc, $"Тип хранения: {item.Storage_Type ?? "Не указан"}");
                AddSeparator(doc);
            }
        }

        private static void AddSectionTitle(Word.Document doc, string title)
        {
            Word.Paragraph sectionTitle = doc.Paragraphs.Add();
            Word.Range range = sectionTitle.Range;
            range.Text = title;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 14;
            range.Font.Bold = 1;
            range.Font.Underline = Word.WdUnderline.wdUnderlineSingle;
            sectionTitle.Format.SpaceBefore = 12;
            sectionTitle.Format.SpaceAfter = 6;
            sectionTitle.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            range.InsertParagraphAfter();
        }

        private static void AddTextParagraph(Word.Document doc, string text, bool bold = false)
        {
            Word.Paragraph paragraph = doc.Paragraphs.Add();
            Word.Range range = paragraph.Range;
            range.Text = text;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 12;
            range.Font.Bold = bold ? 1 : 0;
            paragraph.Format.SpaceBefore = 0;
            paragraph.Format.SpaceAfter = 0;
            paragraph.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            paragraph.Format.FirstLineIndent = 20;
            range.InsertParagraphAfter();
        }

        private static void AddSeparator(Word.Document doc, int spaceAfter = 0)
        {
            Word.Paragraph separator = doc.Paragraphs.Add();
            Word.Range range = separator.Range;
            range.Text = string.Empty;
            range.ParagraphFormat.Borders[Word.WdBorderType.wdBorderBottom].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            range.ParagraphFormat.Borders[Word.WdBorderType.wdBorderBottom].LineWidth = Word.WdLineWidth.wdLineWidth050pt;
            range.ParagraphFormat.SpaceAfter = spaceAfter;
            range.InsertParagraphAfter();
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