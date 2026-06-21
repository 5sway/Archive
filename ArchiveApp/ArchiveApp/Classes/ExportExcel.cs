using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace ArchiveApp
{
    /// <summary>
    /// Экспорт данных в формат Excel.
    /// Формирует отдельные листы для каждой выбранной таблицы с заголовками и данными.
    /// Включает вложенные данные (подписи, запросы, сканы) для полноты отчёта.
    /// </summary>
    class ExportExcel
    {
        public static void ExportToExcel(string filePath, List<string> selectedTables,
            Dictionary<string, List<int>> selectedRecordIds, string userRole,
            DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || selectedTables == null || string.IsNullOrEmpty(userRole))
                {
                    MessageBox.Show("Ошибка: некорректные параметры экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var context = new ArchiveBaseEntities())
                {
                    List<int> documentIds = new List<int>();
                    List<int> requestIds = new List<int>();
                    List<int> userIds = new List<int>();
                    List<int> cardIds = new List<int>();

                    if (selectedRecordIds != null)
                    {
                        if (selectedRecordIds.ContainsKey("Documents") && selectedRecordIds["Documents"].Any())
                            documentIds = selectedRecordIds["Documents"];
                        if (selectedRecordIds.ContainsKey("Requests") && selectedRecordIds["Requests"].Any())
                            requestIds = selectedRecordIds["Requests"];
                        if (selectedRecordIds.ContainsKey("Users") && selectedRecordIds["Users"].Any())
                            userIds = selectedRecordIds["Users"];
                        if (selectedRecordIds.ContainsKey("RegistrationCards") && selectedRecordIds["RegistrationCards"].Any())
                            cardIds = selectedRecordIds["RegistrationCards"];
                    }
                    var documentsQuery = context.Document.AsQueryable();
                    if (documentIds.Any())
                        documentsQuery = documentsQuery.Where(d => documentIds.Contains(d.Id));
                    if (startDate.HasValue && endDate.HasValue)
                        documentsQuery = documentsQuery.Where(d => d.Receipt_Date >= startDate && d.Receipt_Date <= endDate);
                    var documents = documentsQuery.ToList();

                    var requestsQuery = context.Request.Include("User").Include("Document").AsQueryable();
                    if (requestIds.Any())
                        requestsQuery = requestsQuery.Where(r => requestIds.Contains(r.Id));
                    if (startDate.HasValue && endDate.HasValue)
                        requestsQuery = requestsQuery.Where(r => r.Request_Date >= startDate && r.Request_Date <= endDate);
                    var requests = requestsQuery.ToList();

                    var usersQuery = context.User.Include("Role").AsQueryable();
                    if (userIds.Any())
                        usersQuery = usersQuery.Where(u => userIds.Contains(u.Id));
                    var users = usersQuery.ToList();

                    var regCardsQuery = context.Registration_Card.Include("User").Include("Document").AsQueryable();
                    if (cardIds.Any())
                        regCardsQuery = regCardsQuery.Where(c => cardIds.Contains(c.Id));
                    if (startDate.HasValue && endDate.HasValue)
                        regCardsQuery = regCardsQuery.Where(c => c.Registration_Date >= startDate && c.Registration_Date <= endDate);
                    var regCards = regCardsQuery.ToList();

                    var roles = context.Role.ToList();

                    if (userRole == "Делопроизводитель")
                    {
                        selectedTables.Remove("Requests");
                    }

                    if (!selectedTables.Any())
                    {
                        MessageBox.Show("Нет данных для экспорта!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    Excel.Application excelApp = new Excel.Application();
                    excelApp.Visible = false;
                    Excel.Workbook workbook = excelApp.Workbooks.Add();

                    while (workbook.Sheets.Count > 1)
                        ((Excel.Worksheet)workbook.Sheets[workbook.Sheets.Count]).Delete();

                    int sheetIndex = 1;
                    foreach (var table in selectedTables)
                    {
                        Excel.Worksheet sheet;
                        if (sheetIndex == 1)
                            sheet = (Excel.Worksheet)workbook.Sheets[1];
                        else
                            sheet = (Excel.Worksheet)workbook.Sheets.Add(After: workbook.Sheets[workbook.Sheets.Count]);

                        switch (table)
                        {
                            case "Documents":
                                if (documents.Any())
                                {
                                    sheet.Name = "Документы";
                                    ExportDocumentsToExcel(sheet, documents, requests, regCards, users);
                                }
                                break;
                            case "Requests":
                                if (requests.Any())
                                {
                                    sheet.Name = "Запросы";
                                    ExportRequestsToExcel(sheet, requests, documents, users);
                                }
                                break;
                            case "Users":
                                if (users.Any())
                                {
                                    sheet.Name = "Пользователи";
                                    ExportUsersToExcel(sheet, users, roles);
                                }
                                break;
                            case "RegistrationCards":
                                if (regCards.Any())
                                {
                                    sheet.Name = "Рег. карты";
                                    ExportRegistrationCardsToExcel(sheet, regCards, documents, users);
                                }
                                break;
                            default:
                                MessageBox.Show($"Неизвестная таблица: {table}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                break;
                        }
                        sheetIndex++;
                    }

                    for (int i = workbook.Sheets.Count; i > selectedTables.Count; i--)
                        ((Excel.Worksheet)workbook.Sheets[i]).Delete();

                    workbook.SaveAs(filePath);
                    workbook.Close();
                    excelApp.Quit();

                    ReleaseExcelObjects(workbook, excelApp);
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}\nStackTrace: {ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ExportDocumentsToExcel(Excel.Worksheet sheet, List<Document> documents,
    List<Request> requests, List<Registration_Card> regCards, List<User> users)
        {
            int row = 1;
            sheet.Cells[row, 1] = "ДОКУМЕНТЫ";
            sheet.Cells[row, 1].Font.Bold = true;
            sheet.Cells[row, 1].Font.Size = 16;
            row += 2;
            string[] docHeaders = {
        "ID",
        "Арх. шифр",
        "Дата получения",
        "Название",
        "Источник",
        "Копий",
        "Тип",
        "Полка",
        "Срок хранения",
        "Кол-во сканов",
        "Путь к сканам"
    };

            SetHeaders(sheet, row, docHeaders);
            row++;

            foreach (var doc in documents)
            {
                sheet.Cells[row, 1] = doc.Id;
                sheet.Cells[row, 2] = doc.Number ?? "";
                sheet.Cells[row, 3] = doc.Receipt_Date.ToShortDateString();
                sheet.Cells[row, 4] = doc.Title ?? "";
                sheet.Cells[row, 5] = doc.Source ?? "";
                sheet.Cells[row, 6] = doc.Copies_Count;
                sheet.Cells[row, 7] = doc.Storage_Type ?? "";
                sheet.Cells[row, 8] = doc.Shelf_Number ?? "-";
                int termYears = doc.Storage_Type?.Contains("Электронный") == true ? 5 : 10;
                DateTime endDate = doc.Receipt_Date.AddYears(termYears);
                sheet.Cells[row, 9] = $"{termYears} лет (до {endDate:dd.MM.yyyy})";

                var attachments = DocumentAttachmentService.GetAttachments(doc.Id);
                sheet.Cells[row, 10] = attachments.Count;

                if (attachments.Any())
                {
                    string paths = string.Join("\n", attachments.Select(a => a.FilePath));
                    sheet.Cells[row, 11] = paths;
                }
                else
                {
                    sheet.Cells[row, 11] = "—";
                }

                row++;
            }

            row += 2;

            foreach (var doc in documents)
            {
                var regCard = regCards.FirstOrDefault(r => r.Document_Id == doc.Id);
                if (regCard != null)
                {
                    sheet.Cells[row, 1] = $"Подпись документа \"{doc.Title}\"";
                    sheet.Cells[row, 1].Font.Bold = true;
                    sheet.Cells[row, 1].Font.Size = 14;
                    row++;

                    string[] subHeaders = { "Дата регистрации", "Статус", "Подписал" };
                    SetHeaders(sheet, row, subHeaders);
                    row++;

                    var user = users.FirstOrDefault(u => u.Id == regCard.User_Id);
                    sheet.Cells[row, 1] = regCard.Registration_Date.ToShortDateString();
                    sheet.Cells[row, 2] = regCard.Signature ? "Подписан" : "Не подписан";
                    sheet.Cells[row, 3] = user != null ? $"{user.Last_Name} {user.Name} {user.First_Name}" : "Неизвестно";
                    row += 2;
                }

                var docReqs = requests.Where(r => r.Document_Id == doc.Id).ToList();
                if (docReqs.Any())
                {
                    sheet.Cells[row, 1] = $"Запросы по документу \"{doc.Title}\"";
                    sheet.Cells[row, 1].Font.Bold = true;
                    sheet.Cells[row, 1].Font.Size = 14;
                    row++;

                    string[] reqHeaders = { "Дата запроса", "Причина", "Статус", "Запросил" };
                    SetHeaders(sheet, row, reqHeaders);
                    row++;

                    foreach (var req in docReqs)
                    {
                        sheet.Cells[row, 1] = req.Request_Date.ToShortDateString();
                        sheet.Cells[row, 2] = req.Reason ?? "";
                        sheet.Cells[row, 3] = req.Status == true ? "Принято" : "Отклонено";
                        sheet.Cells[row, 4] = req.User != null ? $"{req.User.Last_Name} {req.User.Name}" : "—";
                        row++;
                    }
                    row += 2;
                }
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportRequestsToExcel(Excel.Worksheet sheet, List<Request> requests,
            List<Document> documents, List<User> users)
        {
            int row = 1;

            sheet.Cells[row, 1] = "ЗАПРОСЫ";
            sheet.Cells[row, 1].Font.Bold = true;
            sheet.Cells[row, 1].Font.Size = 16;
            row += 2;

            string[] reqHeaders = { "ID", "Дата запроса", "Причина", "Статус", "Запросил", "Документ" };
            SetHeaders(sheet, row, reqHeaders);
            row++;

            foreach (var req in requests)
            {
                sheet.Cells[row, 1] = req.Id;
                sheet.Cells[row, 2] = req.Request_Date.ToShortDateString();
                sheet.Cells[row, 3] = req.Reason ?? "";
                sheet.Cells[row, 4] = req.Status == true ? "Принято" : "Отклонено";
                sheet.Cells[row, 5] = req.User != null ? $"{req.User.Last_Name} {req.User.Name}" : "—";
                sheet.Cells[row, 6] = req.Document?.Title ?? "—";
                row++;
            }

            row += 2;

            foreach (var req in requests)
            {
                if (req.Document != null)
                {
                    sheet.Cells[row, 1] = $"Документ запроса \"{req.Document.Title}\"";
                    sheet.Cells[row, 1].Font.Bold = true;
                    sheet.Cells[row, 1].Font.Size = 14;
                    row++;

                    string[] docHeaders = { "Арх. шифр", "Дата", "Источник", "Копий", "Тип", "Полка" };
                    SetHeaders(sheet, row, docHeaders);
                    row++;

                    sheet.Cells[row, 1] = req.Document.Number ?? "";
                    sheet.Cells[row, 2] = req.Document.Receipt_Date.ToShortDateString();
                    sheet.Cells[row, 3] = req.Document.Source ?? "";
                    sheet.Cells[row, 4] = req.Document.Copies_Count;
                    sheet.Cells[row, 5] = req.Document.Storage_Type ?? "";
                    sheet.Cells[row, 6] = req.Document.Shelf_Number ?? "-";
                    row += 2;
                }
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportUsersToExcel(Excel.Worksheet sheet, List<User> users, List<Role> roles)
        {
            int row = 1;

            sheet.Cells[row, 1] = "ПОЛЬЗОВАТЕЛИ";
            sheet.Cells[row, 1].Font.Bold = true;
            sheet.Cells[row, 1].Font.Size = 16;
            row += 2;

            string[] headers = { "ID", "Логин", "ФИО", "Роль", "Email", "Телефон" };
            SetHeaders(sheet, row, headers);
            row++;

            foreach (var user in users)
            {
                sheet.Cells[row, 1] = user.Id;
                sheet.Cells[row, 2] = user.Login ?? "";
                sheet.Cells[row, 3] = $"{user.Last_Name} {user.Name} {user.First_Name}".Trim();
                sheet.Cells[row, 4] = user.Role?.Name ?? "—";
                sheet.Cells[row, 5] = user.Email ?? "";
                sheet.Cells[row, 6] = user.Phone_Number ?? "";
                row++;
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportRegistrationCardsToExcel(Excel.Worksheet sheet, List<Registration_Card> cards,
            List<Document> documents, List<User> users)
        {
            int row = 1;

            sheet.Cells[row, 1] = "РЕГИСТРАЦИОННЫЕ КАРТЫ";
            sheet.Cells[row, 1].Font.Bold = true;
            sheet.Cells[row, 1].Font.Size = 16;
            row += 2;

            string[] headers = { "ID", "Дата регистрации", "Статус", "Подписал", "Документ" };
            SetHeaders(sheet, row, headers);
            row++;

            foreach (var card in cards)
            {
                sheet.Cells[row, 1] = card.Id;
                sheet.Cells[row, 2] = card.Registration_Date.ToShortDateString();
                sheet.Cells[row, 3] = card.Signature ? "Подписан" : "Не подписан";
                sheet.Cells[row, 4] = card.User != null ? $"{card.User.Last_Name} {card.User.Name}" : "—";
                sheet.Cells[row, 5] = card.Document?.Title ?? "—";
                row++;
            }

            row += 2;

            foreach (var card in cards)
            {
                if (card.Document != null)
                {
                    sheet.Cells[row, 1] = $"Документ карты \"{card.Document.Title}\"";
                    sheet.Cells[row, 1].Font.Bold = true;
                    sheet.Cells[row, 1].Font.Size = 14;
                    row++;

                    string[] docHeaders = { "Арх. шифр", "Дата", "Источник", "Копий", "Тип", "Полка" };
                    SetHeaders(sheet, row, docHeaders);
                    row++;

                    sheet.Cells[row, 1] = card.Document.Number ?? "";
                    sheet.Cells[row, 2] = card.Document.Receipt_Date.ToShortDateString();
                    sheet.Cells[row, 3] = card.Document.Source ?? "";
                    sheet.Cells[row, 4] = card.Document.Copies_Count;
                    sheet.Cells[row, 5] = card.Document.Storage_Type ?? "";
                    sheet.Cells[row, 6] = card.Document.Shelf_Number ?? "-";
                    row += 2;
                }
            }

            FormatExcelSheet(sheet);
        }

        private static void SetHeaders(Excel.Worksheet sheet, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[row, i + 1] = headers[i];
                sheet.Cells[row, i + 1].Font.Bold = true;
                sheet.Cells[row, i + 1].Font.Name = "Times New Roman";
                sheet.Cells[row, i + 1].Font.Size = 12;
                sheet.Cells[row, i + 1].Interior.Color = Excel.XlRgbColor.rgbLightGray;
            }
        }

        private static void FormatExcelSheet(Excel.Worksheet sheet)
        {
            sheet.Columns.AutoFit();
            Excel.Range allCells = sheet.UsedRange;
            allCells.Font.Name = "Times New Roman";
            allCells.Font.Size = 11;
            allCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            allCells.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
            allCells.Borders.Weight = Excel.XlBorderWeight.xlThin;
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
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void ReleaseExcelObjects(params object[] objects)
        {
            foreach (var obj in objects)
            {
                try
                {
                    if (System.Runtime.InteropServices.Marshal.IsComObject(obj))
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
                catch { }
                finally
                {
                    GC.Collect();
                }
            }
        }
    }
}