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
        public static void ExportToExcel(string filePath, string userRole)
        {
            try
            {
                using (var context = new ArchiveBaseEntities()) // Подключение к базе данных
                {
                    var documents = context.Document.ToList(); // Загрузка документов
                    var requests = context.Request.Include("User").Include("Document").ToList(); // Загрузка запросов
                    var regCards = context.Registration_Card.Include("User").Include("Document").ToList(); // Загрузка карточек

                    if (File.Exists(filePath)) File.Delete(filePath); // Удаление существующего файла

                    Excel.Application excelApp = new Excel.Application(); // Создание приложения Excel
                    excelApp.Visible = false;           // Скрытие интерфейса Excel
                    Excel.Workbook workbook = excelApp.Workbooks.Add(); // Создание новой книги

                    while (workbook.Sheets.Count > 1)   // Удаление лишних листов
                        ((Excel.Worksheet)workbook.Sheets[2]).Delete();

                    // Экспорт в зависимости от роли пользователя
                    ExportDocumentsToExcel(workbook, documents); // Лист "Документы" доступен всем

                    if (userRole != "Делопроизводитель") // Лист "Запросы" только для не-делопроизводителей
                        ExportRequestsToExcel(workbook, requests);

                    ExportRegistrationCardsToExcel(workbook, regCards); // Лист "Рег. карты" доступен всем

                    workbook.SaveAs(filePath);          // Сохранение книги
                    workbook.Close();                   // Закрытие книги
                    excelApp.Quit();                    // Закрытие приложения Excel

                    ReleaseExcelObjects(workbook, excelApp); // Освобождение ресурсов
                    OpenExportedFile(filePath);         // Открытие файла
                }
            }
            catch (Exception ex)                        // Обработка ошибок
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private static void ExportDocumentsToExcel(Excel.Workbook workbook, List<Document> documents)
        {
            Excel.Worksheet docSheet = (Excel.Worksheet)workbook.Sheets[1]; // Первый лист для документов
            docSheet.Name = "Документы";            // Установка имени листа

            docSheet.Cells.Font.Name = "Times New Roman"; // Установка шрифта
            docSheet.Cells.Font.Size = 12;          // Установка размера шрифта

            // Установка заголовков столбцов
            docSheet.Cells[1, 1] = "ID";
            docSheet.Cells[1, 2] = "Номер";
            docSheet.Cells[1, 3] = "Дата получения";
            docSheet.Cells[1, 4] = "Название";
            docSheet.Cells[1, 5] = "Источник";
            docSheet.Cells[1, 6] = "Копии";
            docSheet.Cells[1, 7] = "Тип хранения";

            for (int i = 0; i < documents.Count; i++) // Заполнение данными
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

            FormatExcelSheet(docSheet);             // Форматирование листа
        }

        private static void ExportRequestsToExcel(Excel.Workbook workbook, List<Request> requests)
        {
            Excel.Worksheet reqSheet = (Excel.Worksheet)workbook.Sheets.Add(); // Новый лист для запросов
            reqSheet.Name = "Запросы";              // Установка имени листа
            reqSheet.Cells.Font.Name = "Times New Roman"; // Установка шрифта
            reqSheet.Cells.Font.Size = 12;          // Установка размера шрифта

            // Установка заголовков столбцов
            reqSheet.Cells[1, 1] = "ID";
            reqSheet.Cells[1, 2] = "Дата запроса";
            reqSheet.Cells[1, 3] = "Причина";
            reqSheet.Cells[1, 4] = "Статус";
            reqSheet.Cells[1, 5] = "Запросил";
            reqSheet.Cells[1, 6] = "Документ";

            for (int i = 0; i < requests.Count; i++) // Заполнение данными
            {
                var req = requests[i];
                reqSheet.Cells[i + 2, 1] = req.Id;
                reqSheet.Cells[i + 2, 2] = req.Request_Date.ToShortDateString();
                reqSheet.Cells[i + 2, 3] = req.Reason;
                reqSheet.Cells[i + 2, 4] = req.Status == true ? "Подтвержден" : "Отклонен";
                reqSheet.Cells[i + 2, 5] = req.User?.Name ?? "Неизвестно";
                reqSheet.Cells[i + 2, 6] = req.Document?.Title ?? "Неизвестно";
            }

            FormatExcelSheet(reqSheet);             // Форматирование листа
        }

        private static void ExportRegistrationCardsToExcel(Excel.Workbook workbook, List<Registration_Card> regCards)
        {
            Excel.Worksheet regSheet = (Excel.Worksheet)workbook.Sheets.Add(); // Новый лист для карточек
            regSheet.Name = "Рег. карты";           // Установка имени листа
            regSheet.Cells.Font.Name = "Times New Roman"; // Установка шрифта
            regSheet.Cells.Font.Size = 12;          // Установка размера шрифта

            // Установка заголовков столбцов
            regSheet.Cells[1, 1] = "ID";
            regSheet.Cells[1, 2] = "Дата регистрации";
            regSheet.Cells[1, 3] = "Подпись";
            regSheet.Cells[1, 4] = "Подписал";
            regSheet.Cells[1, 5] = "Документ";

            for (int i = 0; i < regCards.Count; i++) // Заполнение данными
            {
                var reg = regCards[i];
                regSheet.Cells[i + 2, 1] = reg.Id;
                regSheet.Cells[i + 2, 2] = reg.Registration_Date.ToShortDateString();
                regSheet.Cells[i + 2, 3] = reg.Signature ? "Подписано" : "Не подписано";
                regSheet.Cells[i + 2, 4] = reg.User?.Name ?? "Неизвестно";
                regSheet.Cells[i + 2, 5] = reg.Document?.Title ?? "Неизвестно";
            }

            FormatExcelSheet(regSheet);             // Форматирование листа
        }

        private static void FormatExcelSheet(Excel.Worksheet sheet)
        {
            sheet.Columns.AutoFit();                // Автоподбор ширины столбцов

            Excel.Range headerRange = sheet.Range["A1", GetExcelColumnName(sheet.UsedRange.Columns.Count) + "1"]; // Диапазон заголовков
            headerRange.Font.Bold = true;           // Жирный шрифт для заголовков
            headerRange.Font.Name = "Times New Roman"; // Установка шрифта
            headerRange.Font.Size = 12;             // Установка размера шрифта
            headerRange.Interior.Color = Excel.XlRgbColor.rgbLightGray; // Серый фон

            Excel.Range allCells = sheet.UsedRange; // Все используемые ячейки
            allCells.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter; // Выравнивание по горизонтали
            allCells.VerticalAlignment = Excel.XlVAlign.xlVAlignCenter; // Выравнивание по вертикали
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";                 // Имя столбца
            while (columnNumber > 0)                // Преобразование номера в буквы
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;                      // Возврат имени столбца (A, B, ..., AA, AB и т.д.)
        }

        private static void ReleaseExcelObjects(params object[] objects)
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