using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace ArchiveApp
{
    class ExportExcel
    {
        public static void ExportToExcel(string filePath, List<string> selectedTables, string userRole, DateTime? startDate, DateTime? endDate)
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
                    var documents = context.Document.ToList() ?? new List<Document>();
                    var requests = context.Request.Include("User").Include("Document").ToList() ?? new List<Request>();
                    var users = context.User.Include("Role").ToList() ?? new List<User>();
                    var regCards = context.Registration_Card.Include("User").Include("Document").ToList() ?? new List<Registration_Card>();

                    // Фильтрация по периоду, если указан
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        documents = documents.Where(d => d.Receipt_Date >= startDate && d.Receipt_Date <= endDate).ToList();
                        requests = requests.Where(r => r.Request_Date >= startDate && r.Request_Date <= endDate).ToList();
                        regCards = regCards.Where(c => c.Registration_Date >= startDate && c.Registration_Date <= endDate).ToList();
                    }

                    // Учет роли: делопроизводитель не видит запросы
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

                    // Удаление лишних листов, оставляем один
                    while (workbook.Sheets.Count > 1)
                        ((Excel.Worksheet)workbook.Sheets[workbook.Sheets.Count]).Delete();

                    // Обрабатываем каждую таблицу
                    int sheetIndex = 1;
                    foreach (var table in selectedTables)
                    {
                        Excel.Worksheet sheet;
                        if (sheetIndex == 1)
                        {
                            sheet = (Excel.Worksheet)workbook.Sheets[1]; // Используем первый лист
                        }
                        else
                        {
                            sheet = (Excel.Worksheet)workbook.Sheets.Add(After: workbook.Sheets[workbook.Sheets.Count]);
                        }

                        switch (table)
                        {
                            case "Documents":
                                if (documents.Any())
                                {
                                    sheet.Name = "Документы";
                                    ExportDocumentsToExcel(sheet, documents);
                                }
                                break;
                            case "Requests":
                                if (requests.Any())
                                {
                                    sheet.Name = "Запросы";
                                    ExportRequestsToExcel(sheet, requests);
                                }
                                break;
                            case "Users":
                                if (users.Any())
                                {
                                    sheet.Name = "Пользователи";
                                    ExportUsersToExcel(sheet, users);
                                }
                                break;
                            case "RegistrationCards":
                                if (regCards.Any())
                                {
                                    sheet.Name = "Рег. карты";
                                    ExportRegistrationCardsToExcel(sheet, regCards);
                                }
                                break;
                            default:
                                MessageBox.Show($"Неизвестная таблица: {table}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                break;
                        }
                        sheetIndex++;
                    }

                    // Удаляем пустые листы, если они остались
                    for (int i = workbook.Sheets.Count; i > selectedTables.Count; i--)
                    {
                        ((Excel.Worksheet)workbook.Sheets[i]).Delete();
                    }

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

        private static void ExportDocumentsToExcel(Excel.Worksheet sheet, List<Document> documents)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID";
            sheet.Cells[1, 2] = "Номер";
            sheet.Cells[1, 3] = "Дата получения";
            sheet.Cells[1, 4] = "Название";
            sheet.Cells[1, 5] = "Источник";
            sheet.Cells[1, 6] = "Копии";
            sheet.Cells[1, 7] = "Тип хранения";

            for (int i = 0; i < documents.Count; i++)
            {
                var doc = documents[i];
                sheet.Cells[i + 2, 1] = doc.Id;
                sheet.Cells[i + 2, 2] = doc.Number ?? "";
                sheet.Cells[i + 2, 3] = doc.Receipt_Date.ToShortDateString() ?? "";
                sheet.Cells[i + 2, 4] = doc.Title ?? "";
                sheet.Cells[i + 2, 5] = doc.Source ?? "";
                sheet.Cells[i + 2, 6] = doc.Copies_Count;
                sheet.Cells[i + 2, 7] = doc.Storage_Type ?? "";
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportRequestsToExcel(Excel.Worksheet sheet, List<Request> requests)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID";
            sheet.Cells[1, 2] = "Дата запроса";
            sheet.Cells[1, 3] = "Причина";
            sheet.Cells[1, 4] = "Статус";
            sheet.Cells[1, 5] = "Запросил";
            sheet.Cells[1, 6] = "Документ";

            for (int i = 0; i < requests.Count; i++)
            {
                var req = requests[i];
                sheet.Cells[i + 2, 1] = req.Id;
                sheet.Cells[i + 2, 2] = req.Request_Date.ToShortDateString() ?? "";
                sheet.Cells[i + 2, 3] = req.Reason ?? "";
                sheet.Cells[i + 2, 4] = req.Status == true ? "Подтвержден" : "Отклонен";
                sheet.Cells[i + 2, 5] = req.User?.Name ?? "Неизвестно";
                sheet.Cells[i + 2, 6] = req.Document?.Title ?? "Неизвестно";
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportUsersToExcel(Excel.Worksheet sheet, List<User> users)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID";
            sheet.Cells[1, 2] = "Логин";
            sheet.Cells[1, 3] = "Имя";
            sheet.Cells[1, 4] = "Фамилия";
            sheet.Cells[1, 5] = "Отчество";
            sheet.Cells[1, 6] = "Роль";
            sheet.Cells[1, 7] = "Email";
            sheet.Cells[1, 8] = "Телефон";

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                sheet.Cells[i + 2, 1] = user.Id;
                sheet.Cells[i + 2, 2] = user.Login ?? "";
                sheet.Cells[i + 2, 3] = user.Name ?? "";
                sheet.Cells[i + 2, 4] = user.Last_Name ?? "";
                sheet.Cells[i + 2, 5] = user.First_Name ?? "";
                sheet.Cells[i + 2, 6] = user.Role?.Name ?? "Неизвестно";
                sheet.Cells[i + 2, 7] = user.Email ?? "";
                sheet.Cells[i + 2, 8] = user.Phone_Number ?? "";
            }

            FormatExcelSheet(sheet);
        }

        private static void ExportRegistrationCardsToExcel(Excel.Worksheet sheet, List<Registration_Card> regCards)
        {
            sheet.Cells.Font.Name = "Times New Roman";
            sheet.Cells.Font.Size = 12;

            sheet.Cells[1, 1] = "ID";
            sheet.Cells[1, 2] = "Дата регистрации";
            sheet.Cells[1, 3] = "Подпись";
            sheet.Cells[1, 4] = "Подписал";
            sheet.Cells[1, 5] = "Документ";

            for (int i = 0; i < regCards.Count; i++)
            {
                var reg = regCards[i];
                sheet.Cells[i + 2, 1] = reg.Id;
                sheet.Cells[i + 2, 2] = reg.Registration_Date.ToShortDateString() ?? "";
                sheet.Cells[i + 2, 3] = reg.Signature ? "Подписано" : "Не подписано";
                sheet.Cells[i + 2, 4] = reg.User?.Name ?? "Неизвестно";
                sheet.Cells[i + 2, 5] = reg.Document?.Title ?? "Неизвестно";
            }

            FormatExcelSheet(sheet);
        }

        private static void FormatExcelSheet(Excel.Worksheet sheet)
        {
            sheet.Columns.AutoFit();
            Excel.Range headerRange = sheet.Range["A1", GetExcelColumnName(sheet.UsedRange.Columns.Count) + "1"];
            headerRange.Font.Bold = true;
            headerRange.Font.Name = "Times New Roman";
            headerRange.Font.Size = 12;
            headerRange.Interior.Color = Excel.XlRgbColor.rgbLightGray;
            Excel.Range allCells = sheet.UsedRange;
            allCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            allCells.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        private static void ReleaseExcelObjects(params object[] objects)
        {
            foreach (var obj in objects)
            {
                try
                {
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