using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Excel = Microsoft.Office.Interop.Excel;

namespace ArchiveApp
{
    class ExportExcel
    {
        public static void ExportToExcel(string filePath)
        {
            // Основной метод экспорта данных в Excel
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    // Получаем данные из базы
                    var documents = context.Document.ToList();
                    var requests = context.Request.Include("User").Include("Document").ToList();
                    var regCards = context.Registration_Card.Include("User").Include("Document").ToList();

                    // Удаляем файл, если он уже существует
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // Создаем Excel приложение
                    Excel.Application excelApp = new Excel.Application();
                    excelApp.Visible = false;

                    // Создаем новую книгу
                    Excel.Workbook workbook = excelApp.Workbooks.Add();

                    // Удаляем лишние листы (оставляем только 1)
                    while (workbook.Sheets.Count > 1)
                    {
                        ((Excel.Worksheet)workbook.Sheets[2]).Delete();
                    }

                    // Экспортируем данные на разные листы
                    ExportDocumentsToExcel(workbook, documents);
                    ExportRequestsToExcel(workbook, requests);
                    ExportRegistrationCardsToExcel(workbook, regCards);

                    // Сохраняем и закрываем книгу
                    workbook.SaveAs(filePath);
                    workbook.Close();
                    excelApp.Quit();

                    // Освобождаем ресурсы
                    ReleaseExcelObjects(workbook, excelApp);

                    // Открываем экспортированный файл
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка",
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

        private static void ExportDocumentsToExcel(Excel.Workbook workbook, List<Document> documents)
        {
            // Экспорт документов на лист Excel
            Excel.Worksheet docSheet = (Excel.Worksheet)workbook.Sheets[1];
            docSheet.Name = "Документы";

            // Настройка шрифта
            docSheet.Cells.Font.Name = "Times New Roman";
            docSheet.Cells.Font.Size = 12;

            // Заголовки столбцов
            docSheet.Cells[1, 1] = "ID";
            docSheet.Cells[1, 2] = "Номер";
            docSheet.Cells[1, 3] = "Дата получения";
            docSheet.Cells[1, 4] = "Название";
            docSheet.Cells[1, 5] = "Источник";
            docSheet.Cells[1, 6] = "Копии";
            docSheet.Cells[1, 7] = "Тип хранения";

            // Заполнение данными
            for (int i = 0; i < documents.Count; i++)
            {
                var doc = documents[i];
                docSheet.Cells[i + 2, 1] = doc.Id;
                docSheet.Cells[i + 2, 2] = doc.Number;
                docSheet.Cells[i + 2, 3] = doc.Receipt_Date.ToShortDateString();
                docSheet.Cells[i + 2, 4] = doc.Title;
                docSheet.Cells[i + 2, 5] = doc.Source;
                docSheet.Cells[i + 2, 6] = doc.Copies_Count;
                docSheet.Cells[i + 2, 7] = doc.Storage_Type;
            }

            // Форматирование листа
            FormatExcelSheet(docSheet);
        }

        private static void ExportRequestsToExcel(Excel.Workbook workbook, List<Request> requests)
        {
            Excel.Worksheet reqSheet = (Excel.Worksheet)workbook.Sheets.Add();
            reqSheet.Name = "Запросы";
            reqSheet.Cells.Font.Name = "Times New Roman";
            reqSheet.Cells.Font.Size = 12;
            reqSheet.Cells[1, 1] = "ID";
            reqSheet.Cells[1, 2] = "Дата запроса";
            reqSheet.Cells[1, 3] = "Причина";
            reqSheet.Cells[1, 4] = "Статус";
            reqSheet.Cells[1, 5] = "Запросил";
            reqSheet.Cells[1, 6] = "Документ";
            for (int i = 0; i < requests.Count; i++)
            {
                var req = requests[i];
                reqSheet.Cells[i + 2, 1] = req.Id;
                reqSheet.Cells[i + 2, 2] = req.Request_Date.ToShortDateString();
                reqSheet.Cells[i + 2, 3] = req.Reason;
                reqSheet.Cells[i + 2, 4] = req.Status == true ? "Подтвержден" : "Отклонен";
                reqSheet.Cells[i + 2, 5] = req.User?.Name ?? "Неизвестно";
                reqSheet.Cells[i + 2, 6] = req.Document?.Title ?? "Неизвестно";
            }
            FormatExcelSheet(reqSheet);
        }

        private static void ExportRegistrationCardsToExcel(Excel.Workbook workbook, List<Registration_Card> regCards)
        {
            Excel.Worksheet regSheet = (Excel.Worksheet)workbook.Sheets.Add();
            regSheet.Name = "Рег. карты";
            regSheet.Cells.Font.Name = "Times New Roman";
            regSheet.Cells.Font.Size = 12;
            regSheet.Cells[1, 1] = "ID";
            regSheet.Cells[1, 2] = "Дата регистрации";
            regSheet.Cells[1, 3] = "Подпись";
            regSheet.Cells[1, 4] = "Подписал";
            regSheet.Cells[1, 5] = "Документ";
            for (int i = 0; i < regCards.Count; i++)
            {
                var reg = regCards[i];
                regSheet.Cells[i + 2, 1] = reg.Id;
                regSheet.Cells[i + 2, 2] = reg.Registration_Date.ToShortDateString();
                regSheet.Cells[i + 2, 3] = reg.Signature ? "Подписано" : "Не подписано";
                regSheet.Cells[i + 2, 4] = reg.User?.Name ?? "Неизвестно";
                regSheet.Cells[i + 2, 5] = reg.Document?.Title ?? "Неизвестно";
            }
            FormatExcelSheet(regSheet);
        }

        private static void FormatExcelSheet(Excel.Worksheet sheet)
        {
            // Автоподбор ширины столбцов
            sheet.Columns.AutoFit();

            // Форматирование заголовков
            Excel.Range headerRange = sheet.Range["A1", GetExcelColumnName(sheet.UsedRange.Columns.Count) + "1"];
            headerRange.Font.Bold = true;
            headerRange.Font.Name = "Times New Roman";
            headerRange.Font.Size = 12;
            headerRange.Interior.Color = Excel.XlRgbColor.rgbLightGray; // Серый фон

            // Выравнивание всех ячеек
            Excel.Range allCells = sheet.UsedRange;
            allCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            allCells.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter;
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            // Конвертация номера столбца в буквенное обозначение (A, B, ..., AA, AB и т.д.)
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
            // Освобождение COM-объектов Excel
            foreach (var obj in objects)
            {
                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
                catch { } // Игнорируем ошибки
                finally
                {
                    GC.Collect(); // Принудительный сбор мусора
                }
            }
        }
    }
}

