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
    /// <summary>
    /// Экспорт данных в формат Word и PDF.
    /// Поддерживает два режима: табличный (структурированные данные) и текстовый (связный текст).
    /// Для текстового режима генерирует повествовательные отчёты по документам с полным описанием.
    /// </summary>
    class ExportWord
    {
        public static void ExportToWord(string filePath, List<string> selectedTables, Dictionary<string, List<int>> selectedRecordIds,
            DateTime? startDate, DateTime? endDate, string userRole, string format, bool isTableFormat)
        {
            Word.Application wordApp = null;
            Word.Document doc = null;
            try
            {
                using (var context = new ArchiveBaseEntities())
                {
                    var currentUser = context.User.Include("Role").FirstOrDefault(u => u.Role.Name == userRole);
                    string compilerInfo = currentUser != null
                        ? $"{currentUser.Role?.Name ?? "Специалист"} {currentUser.Last_Name} {currentUser.Name} {currentUser.First_Name}"
                        : "Составитель не определен";

                    if (!isTableFormat)
                    {
                        if (!selectedTables.Contains("Documents") || selectedTables.Count != 1)
                        {
                            MessageBox.Show("В текстовом формате можно выбрать только таблицу 'Документы' с конкретной записью!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (!selectedRecordIds.ContainsKey("Documents") || !selectedRecordIds["Documents"].Any())
                        {
                            MessageBox.Show("В текстовом формате выберите конкретный документ!",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    List<int> documentIds = new List<int>();
                    if (selectedRecordIds != null && selectedRecordIds.ContainsKey("Documents") && selectedRecordIds["Documents"].Any())
                    {
                        documentIds = selectedRecordIds["Documents"];
                    }

                    if (!isTableFormat)
                    {
                        if (documentIds.Count > 1)
                        {
                            MessageBox.Show("В текстовом формате можно выбрать только один документ. Будет экспортирован первый выбранный.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                            documentIds = documentIds.Take(1).ToList();
                        }
                    }

                    var documents = GetFilteredDocuments(context, documentIds, startDate, endDate);
                    var requests = GetFilteredRequests(context, documentIds, startDate, endDate);
                    var regCards = GetFilteredRegCards(context, documentIds, startDate, endDate);
                    var users = context.User.Include("Role").ToList();
                    var roles = context.Role.ToList();

                    if (File.Exists(filePath)) File.Delete(filePath);

                    wordApp = new Word.Application { Visible = false, DisplayAlerts = WdAlertLevel.wdAlertsNone };
                    doc = wordApp.Documents.Add();

                    if (isTableFormat)
                    {
                        SetupPage(doc, true);

                        bool isFirst = true;
                        foreach (var table in selectedTables)
                        {
                            if (!isFirst)
                            {
                                AddEmptyParagraph(doc, 1);
                            }

                            switch (table)
                            {
                                case "Documents":
                                    if (documents.Any())
                                        ExportDocumentsTableFormat(doc, documents, requests, regCards, users);
                                    break;
                                case "Requests":
                                    if (requests.Any())
                                        ExportRequestsTableFormat(doc, requests, documents, users);
                                    break;
                                case "Users":
                                    if (users.Any())
                                        ExportUsersTableFormat(doc, users);
                                    break;
                                case "RegistrationCards":
                                    if (regCards.Any())
                                        ExportRegistrationCardsTableFormat(doc, regCards, documents, users);
                                    break;
                            }
                            isFirst = false;
                        }
                    }
                    else
                    {
                        SetupPage(doc, false);
                        bool isFirst = true;
                        foreach (var table in selectedTables)
                        {
                            if (!isFirst) AddPageBreak(doc);
                            switch (table)
                            {
                                case "Documents":
                                    if (documents.Any())
                                        ExportDocumentsTextFormat(doc, documents, regCards, requests, users, roles, compilerInfo, startDate, endDate);
                                    break;
                                case "Requests":
                                    if (requests.Any())
                                        ExportRequestsTextFormat(doc, requests, documents, users);
                                    break;
                                case "Users":
                                    if (users.Any())
                                        ExportUsersTextFormat(doc, users, roles);
                                    break;
                                case "RegistrationCards":
                                    if (regCards.Any())
                                        ExportRegistrationCardsTextFormat(doc, regCards, documents, users);
                                    break;
                            }
                            isFirst = false;
                        }
                    }

                    var saveFormat = format.Equals("PDF", StringComparison.OrdinalIgnoreCase)
                        ? WdSaveFormat.wdFormatPDF : WdSaveFormat.wdFormatDocumentDefault;

                    doc.SaveAs2(filePath, saveFormat);
                    doc.Close(false);
                    wordApp.Quit();
                    ReleaseWordObjects(doc, wordApp);
                    OpenExportedFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ReleaseWordObjects(doc, wordApp);
            }
        }

        private static void SetupPage(Word.Document doc, bool isLandscape)
        {
            doc.PageSetup.PaperSize = WdPaperSize.wdPaperA4;
            doc.PageSetup.Orientation = isLandscape ? WdOrientation.wdOrientLandscape : WdOrientation.wdOrientPortrait;

            if (isLandscape)
            {
                doc.PageSetup.LeftMargin = 30f;
                doc.PageSetup.RightMargin = 30f;
                doc.PageSetup.TopMargin = 30f;
                doc.PageSetup.BottomMargin = 30f;
            }
            else
            {
                doc.PageSetup.LeftMargin = 85f;
                doc.PageSetup.RightMargin = 30f;
                doc.PageSetup.TopMargin = 56f;
                doc.PageSetup.BottomMargin = 56f;
            }
        }

        private static void AddTitlePage(Word.Document doc, string compilerInfo, DateTime? startDate, DateTime? endDate)
        {
            AddCenteredParagraph(doc, "МИНИСТЕРСТВО ОБРАЗОВАНИЯ И НАУКИ РЕСПУБЛИКИ КОМИ", 14, true);
            AddCenteredParagraph(doc, "ГПОУ \"Воркутинский арктический горно-политехнический колледж\"", 14, true);
            AddEmptyParagraph(doc, 4);

            var title = doc.Paragraphs.Add();
            title.Range.Text = "ОТЧЕТ ПО АРХИВНЫМ ДАННЫМ";
            title.Range.Font.Name = "Times New Roman";
            title.Range.Font.Size = 18;
            title.Range.Font.Bold = 1;
            title.Range.Font.Color = WdColor.wdColorBlack;
            title.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
            title.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
            title.Range.ParagraphFormat.SpaceAfter = 30;
            title.Range.InsertParagraphAfter();

            var compilerPara = doc.Paragraphs.Add();
            compilerPara.Range.Text = $"Составитель: {compilerInfo}";
            compilerPara.Range.Font.Name = "Times New Roman";
            compilerPara.Range.Font.Size = 14;
            compilerPara.Range.Font.Color = WdColor.wdColorBlack;
            compilerPara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphRight;
            compilerPara.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
            compilerPara.Range.ParagraphFormat.SpaceAfter = 40;
            compilerPara.Range.InsertParagraphAfter();

            if (startDate.HasValue && endDate.HasValue)
            {
                var period = doc.Paragraphs.Add();
                period.Range.Text = $"За период: {startDate.Value:dd.MM.yyyy} — {endDate.Value:dd.MM.yyyy}";
                period.Range.Font.Name = "Times New Roman";
                period.Range.Font.Size = 14;
                period.Range.Font.Color = WdColor.wdColorBlack;
                period.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                period.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
                period.Range.ParagraphFormat.SpaceAfter = 30;
                period.Range.InsertParagraphAfter();
            }

            AddCityFooter(doc);
        }

        private static void AddCityFooter(Word.Document doc)
        {
            object end = Word.WdUnits.wdStory;
            Word.Range range = doc.Range(ref end);
            range.Collapse(Word.WdCollapseDirection.wdCollapseEnd);

            float currentPos = range.Information[Word.WdInformation.wdVerticalPositionRelativeToPage];
            float pageHeight = doc.PageSetup.PageHeight;
            float topMargin = doc.PageSetup.TopMargin;
            float bottomMargin = doc.PageSetup.BottomMargin;
            float availableHeight = pageHeight - topMargin - bottomMargin - currentPos;
            float lineHeight = 14 * 1.5f;

            Word.Paragraph footerPara = doc.Paragraphs.Add();
            footerPara.Range.Font.Name = "Times New Roman";
            footerPara.Range.Font.Size = 14;
            footerPara.Range.Font.Bold = 0;
            footerPara.Range.Font.Color = WdColor.wdColorBlack;
            footerPara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
            footerPara.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
            footerPara.Range.ParagraphFormat.SpaceAfter = 0;

            if (availableHeight >= lineHeight * 1.5)
            {
                footerPara.Range.Text = "Воркута, 2026";
                footerPara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                footerPara.Range.ParagraphFormat.SpaceBefore = availableHeight - lineHeight - 10;
            }
            else
            {
                footerPara.Range.Text = "";
                footerPara.Range.ParagraphFormat.SpaceBefore = 0;
            }

            footerPara.Range.InsertParagraphAfter();
        }

        /// <summary>
        /// Экспорт документов в текстовом формате.
        /// Создаёт повествовательный отчёт по каждому документу с описанием его реквизитов,
        /// статуса подписи, наличия запросов и другой информации.
        /// </summary>
        private static void ExportDocumentsTextFormat(Word.Document doc, List<Document> documents,
            List<Registration_Card> regCards, List<Request> requests, List<User> users, List<Role> roles,
            string compilerInfo, DateTime? startDate, DateTime? endDate)
        {
            bool isFirst = true;
            foreach (var docItem in documents)
            {
                if (!isFirst)
                {
                    AddPageBreak(doc);
                }

                AddCenteredParagraph(doc, "МИНИСТЕРСТВО ОБРАЗОВАНИЯ И НАУКИ РЕСПУБЛИКИ КОМИ", 14, true);
                AddCenteredParagraph(doc, "ГПОУ \"Воркутинский арктический горно-политехнический колледж\"", 14, true);
                AddEmptyParagraph(doc, 1);

                var titlePara = doc.Paragraphs.Add();
                titlePara.Range.Text = "ОТЧЕТ ПО АРХИВНОМУ ДОКУМЕНТУ";
                titlePara.Range.Font.Name = "Times New Roman";
                titlePara.Range.Font.Size = 16;
                titlePara.Range.Font.Bold = 1;
                titlePara.Range.Font.Color = WdColor.wdColorBlack;
                titlePara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                titlePara.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
                titlePara.Range.ParagraphFormat.SpaceAfter = 18;
                titlePara.Range.InsertParagraphAfter();

                var compilerPara = doc.Paragraphs.Add();
                compilerPara.Range.Text = $"Составитель: {compilerInfo}";
                compilerPara.Range.Font.Name = "Times New Roman";
                compilerPara.Range.Font.Size = 14;
                compilerPara.Range.Font.Color = WdColor.wdColorBlack;
                compilerPara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphRight;
                compilerPara.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
                compilerPara.Range.ParagraphFormat.SpaceAfter = 18;
                compilerPara.Range.InsertParagraphAfter();

                if (startDate.HasValue && endDate.HasValue)
                {
                    var periodPara = doc.Paragraphs.Add();
                    periodPara.Range.Text = $"За период: {startDate.Value:dd.MM.yyyy} — {endDate.Value:dd.MM.yyyy}";
                    periodPara.Range.Font.Name = "Times New Roman";
                    periodPara.Range.Font.Size = 14;
                    periodPara.Range.Font.Color = WdColor.wdColorBlack;
                    periodPara.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                    periodPara.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
                    periodPara.Range.ParagraphFormat.SpaceAfter = 18;
                    periodPara.Range.InsertParagraphAfter();
                }

                AddEmptyParagraph(doc, 1);

                var regCard = regCards.FirstOrDefault(r => r.Document_Id == docItem.Id);
                var user = regCard != null ? users.FirstOrDefault(u => u.Id == regCard.User_Id) : null;
                var docRequests = requests.Where(r => r.Document_Id == docItem.Id).ToList();

                string reportText = $"Архивный документ с шифром {docItem.Number} под наименованием «{docItem.Title}» поступил в архив {docItem.Receipt_Date:dd.MM.yyyy} года из источника «{docItem.Source}». ";

                if (regCard != null)
                {
                    if (regCard.Signature)
                    {
                        reportText += $"Документ зарегистрирован под №{regCard.Id} и размещён на полке {docItem.Shelf_Number ?? "не указана"}. ";
                        reportText += $"Согласно журналу регистрации, {regCard.Registration_Date:dd.MM.yyyy} документ был подписан. ";
                        if (user != null)
                            reportText += $"Подпись поставил {user.Last_Name} {user.Name} {user.First_Name}. ";
                    }
                    else
                    {
                        reportText += "Документ не подписан. ";
                    }
                }
                else
                {
                    reportText += "Регистрационная карта по данному документу отсутствует. ";
                }

                reportText += $"Тип хранения: {docItem.Storage_Type?.ToLower() ?? "не указан"}, количество экземпляров — {docItem.Copies_Count}. ";

                if (docRequests.Any())
                {
                    reportText += "По документу имеются следующие запросы: ";
                    foreach (var req in docRequests)
                    {
                        string initiator = req.User != null ? $"{req.User.Last_Name} {req.User.Name} {req.User.First_Name}".Trim() : "не указан";
                        reportText += $"{req.Request_Date:dd.MM.yyyy} — инициатор {initiator}, причина «{req.Reason ?? "не указана"}», статус — «{(req.Status == true ? "принято" : "отклонено")}»; ";
                    }
                }
                else
                {
                    reportText += "Запросов на проверку наличия документа не поступало. ";
                }

                var mainPara = doc.Paragraphs.Add();
                mainPara.Range.Text = reportText;
                ApplyTextStyle(mainPara);
                mainPara.Range.ParagraphFormat.FirstLineIndent = 35.4f;
                mainPara.Range.InsertParagraphAfter();

                AddEmptyParagraph(doc, 1);
                AddCityFooter(doc);

                isFirst = false;
            }
        }

        private static void ExportRequestsTextFormat(Word.Document doc, List<Request> requests, List<Document> documents, List<User> users)
        {
            AddCenteredParagraph(doc, "ЗАПРОСЫ", 16, true, 12);
            foreach (var req in requests)
            {
                string text = $"Запрос №{req.Id} от {req.Request_Date:dd.MM.yyyy} года. " +
                    $"Причина: «{req.Reason ?? "не указана"}». " +
                    $"Статус: {(req.Status == true ? "ПРИНЯТ" : "ОТКЛОНЕН")}. ";
                if (req.User != null)
                    text += $"Инициатор: {req.User.Last_Name} {req.User.Name} {req.User.First_Name}. ";
                if (req.Document != null)
                    text += $"Документ: «{req.Document.Title}». ";

                var para = doc.Paragraphs.Add();
                para.Range.Text = text;
                ApplyTextStyle(para);
                para.Range.ParagraphFormat.FirstLineIndent = 35.4f;
                para.Range.InsertParagraphAfter();
                AddEmptyParagraph(doc, 1);
            }
            AddCityFooter(doc);
        }

        private static void ExportUsersTextFormat(Word.Document doc, List<User> users, List<Role> roles)
        {
            AddCenteredParagraph(doc, "ПОЛЬЗОВАТЕЛИ", 16, true, 12);
            foreach (var user in users)
            {
                string text = $"{user.Last_Name} {user.Name} {user.First_Name}. " +
                    $"Логин: {user.Login ?? "—"}. " +
                    $"Роль: {user.Role?.Name ?? "—"}. " +
                    $"Email: {user.Email ?? "—"}. " +
                    $"Телефон: {user.Phone_Number ?? "—"}.";

                var para = doc.Paragraphs.Add();
                para.Range.Text = text;
                ApplyTextStyle(para);
                para.Range.ParagraphFormat.FirstLineIndent = 35.4f;
                para.Range.InsertParagraphAfter();
                AddEmptyParagraph(doc, 1);
            }
            AddCityFooter(doc);
        }

        private static void ExportRegistrationCardsTextFormat(Word.Document doc, List<Registration_Card> cards, List<Document> documents, List<User> users)
        {
            AddCenteredParagraph(doc, "РЕГИСТРАЦИОННЫЕ КАРТЫ", 16, true, 12);
            foreach (var card in cards)
            {
                string text = $"Регистрационная карта №{card.Id} от {card.Registration_Date:dd.MM.yyyy}. " +
                    $"Статус: {(card.Signature ? "ПОДПИСАНА" : "НЕ ПОДПИСАНА")}. ";
                if (card.User != null)
                    text += $"Подписал: {card.User.Last_Name} {card.User.Name} {card.User.First_Name}. ";
                if (card.Document != null)
                    text += $"Документ: «{card.Document.Title}». ";

                var para = doc.Paragraphs.Add();
                para.Range.Text = text;
                ApplyTextStyle(para);
                para.Range.ParagraphFormat.FirstLineIndent = 35.4f;
                para.Range.InsertParagraphAfter();
                AddEmptyParagraph(doc, 1);
            }
            AddCityFooter(doc);
        }

        private static void ExportDocumentsTableFormat(Word.Document doc, List<Document> documents, List<Request> requests, List<Registration_Card> regCards, List<User> users)
        {
            AddCenteredParagraph(doc, "ДОКУМЕНТЫ", 14, true, 8);

            string[] headers = { "ID", "Арх. шифр", "Дата получения", "Название", "Источник", "Копий", "Тип", "Полка" };
            var table = CreateStyledTable(doc, headers);

            int row = 1;
            foreach (var d in documents)
            {
                row++;
                table.Rows.Add();
                table.Cell(row, 1).Range.Text = d.Id.ToString();
                table.Cell(row, 2).Range.Text = d.Number ?? "";
                table.Cell(row, 3).Range.Text = d.Receipt_Date.ToShortDateString();
                table.Cell(row, 4).Range.Text = d.Title ?? "";
                table.Cell(row, 5).Range.Text = d.Source ?? "";
                table.Cell(row, 6).Range.Text = d.Copies_Count.ToString();
                table.Cell(row, 7).Range.Text = d.Storage_Type ?? "";
                table.Cell(row, 8).Range.Text = d.Shelf_Number ?? "-";
                SetTableCellStyle(table, row, headers.Length);
            }

            AddEmptyParagraph(doc, 1);

            foreach (var d in documents)
            {
                var regCard = regCards.FirstOrDefault(r => r.Document_Id == d.Id);
                if (regCard != null)
                {
                    AddSectionTitle(doc, $"Подпись документа \"{d.Title}\"");
                    string[] subHeaders = { "Дата регистрации", "Статус", "Подписал" };
                    var subTable = CreateStyledTable(doc, subHeaders);

                    subTable.Rows.Add();
                    var user = users.FirstOrDefault(u => u.Id == regCard.User_Id);
                    subTable.Cell(2, 1).Range.Text = regCard.Registration_Date.ToShortDateString();
                    subTable.Cell(2, 2).Range.Text = regCard.Signature ? "Подписан" : "Не подписан";
                    subTable.Cell(2, 3).Range.Text = user != null ? $"{user.Last_Name} {user.Name} {user.First_Name}" : "Неизвестно";
                    SetTableCellStyle(subTable, 2, subHeaders.Length);

                    AddEmptyParagraph(doc, 1);
                }

                var docReqs = requests.Where(r => r.Document_Id == d.Id).ToList();
                if (docReqs.Any())
                {
                    AddSectionTitle(doc, $"Запросы по документу \"{d.Title}\"");
                    string[] reqHeaders = { "Дата запроса", "Причина", "Статус", "Запросил" };
                    var reqTable = CreateStyledTable(doc, reqHeaders);

                    int rRow = 1;
                    foreach (var req in docReqs)
                    {
                        rRow++;
                        reqTable.Rows.Add();
                        reqTable.Cell(rRow, 1).Range.Text = req.Request_Date.ToShortDateString();
                        reqTable.Cell(rRow, 2).Range.Text = req.Reason ?? "";
                        reqTable.Cell(rRow, 3).Range.Text = req.Status == true ? "Принято" : "Отклонено";
                        reqTable.Cell(rRow, 4).Range.Text = req.User != null ? $"{req.User.Last_Name} {req.User.Name}" : "—";
                        SetTableCellStyle(reqTable, rRow, reqHeaders.Length);
                    }
                    AddEmptyParagraph(doc, 1);
                }
            }
        }

        private static void ExportRequestsTableFormat(Word.Document doc, List<Request> requests, List<Document> documents, List<User> users)
        {
            AddCenteredParagraph(doc, "ЗАПРОСЫ", 14, true, 8);

            string[] headers = { "ID", "Дата запроса", "Причина", "Статус", "Запросил", "Документ" };
            var table = CreateStyledTable(doc, headers);

            int row = 1;
            foreach (var req in requests)
            {
                row++;
                table.Rows.Add();
                table.Cell(row, 1).Range.Text = req.Id.ToString();
                table.Cell(row, 2).Range.Text = req.Request_Date.ToShortDateString();
                table.Cell(row, 3).Range.Text = req.Reason ?? "";
                table.Cell(row, 4).Range.Text = req.Status == true ? "Принято" : "Отклонено";
                table.Cell(row, 5).Range.Text = req.User != null ? $"{req.User.Last_Name} {req.User.Name}" : "—";
                table.Cell(row, 6).Range.Text = req.Document?.Title ?? "—";
                SetTableCellStyle(table, row, headers.Length);
            }

            AddEmptyParagraph(doc, 1);

            foreach (var req in requests)
            {
                if (req.Document != null)
                {
                    AddSectionTitle(doc, $"Документ запроса \"{req.Document.Title}\"");
                    string[] docHeaders = { "Арх. шифр", "Дата", "Источник", "Копий", "Тип", "Полка" };
                    var docTable = CreateStyledTable(doc, docHeaders);

                    docTable.Rows.Add();
                    docTable.Cell(2, 1).Range.Text = req.Document.Number ?? "";
                    docTable.Cell(2, 2).Range.Text = req.Document.Receipt_Date.ToShortDateString();
                    docTable.Cell(2, 3).Range.Text = req.Document.Source ?? "";
                    docTable.Cell(2, 4).Range.Text = req.Document.Copies_Count.ToString();
                    docTable.Cell(2, 5).Range.Text = req.Document.Storage_Type ?? "";
                    docTable.Cell(2, 6).Range.Text = req.Document.Shelf_Number ?? "-";
                    SetTableCellStyle(docTable, 2, docHeaders.Length);

                    AddEmptyParagraph(doc, 1);
                }
            }
        }

        private static void ExportUsersTableFormat(Word.Document doc, List<User> users)
        {
            AddCenteredParagraph(doc, "ПОЛЬЗОВАТЕЛИ", 14, true, 8);

            string[] headers = { "ID", "Логин", "ФИО", "Роль", "Email", "Телефон" };
            var table = CreateStyledTable(doc, headers);

            int row = 1;
            foreach (var user in users)
            {
                row++;
                table.Rows.Add();
                table.Cell(row, 1).Range.Text = user.Id.ToString();
                table.Cell(row, 2).Range.Text = user.Login ?? "";
                table.Cell(row, 3).Range.Text = $"{user.Last_Name} {user.Name} {user.First_Name}".Trim();
                table.Cell(row, 4).Range.Text = user.Role?.Name ?? "—";
                table.Cell(row, 5).Range.Text = user.Email ?? "";
                table.Cell(row, 6).Range.Text = user.Phone_Number ?? "";
                SetTableCellStyle(table, row, headers.Length);
            }
        }

        private static void ExportRegistrationCardsTableFormat(Word.Document doc, List<Registration_Card> cards, List<Document> documents, List<User> users)
        {
            AddCenteredParagraph(doc, "РЕГИСТРАЦИОННЫЕ КАРТЫ", 14, true, 8);

            string[] headers = { "ID", "Дата регистрации", "Статус", "Подписал", "Документ" };
            var table = CreateStyledTable(doc, headers);

            int row = 1;
            foreach (var card in cards)
            {
                row++;
                table.Rows.Add();
                table.Cell(row, 1).Range.Text = card.Id.ToString();
                table.Cell(row, 2).Range.Text = card.Registration_Date.ToShortDateString();
                table.Cell(row, 3).Range.Text = card.Signature ? "Подписан" : "Не подписан";
                table.Cell(row, 4).Range.Text = card.User != null ? $"{card.User.Last_Name} {card.User.Name}" : "—";
                table.Cell(row, 5).Range.Text = card.Document?.Title ?? "—";
                SetTableCellStyle(table, row, headers.Length);
            }

            AddEmptyParagraph(doc, 1);

            foreach (var card in cards)
            {
                if (card.Document != null)
                {
                    AddSectionTitle(doc, $"Документ карты \"{card.Document.Title}\"");
                    string[] docHeaders = { "Арх. шифр", "Дата", "Источник", "Копий", "Тип", "Полка" };
                    var docTable = CreateStyledTable(doc, docHeaders);

                    docTable.Rows.Add();
                    docTable.Cell(2, 1).Range.Text = card.Document.Number ?? "";
                    docTable.Cell(2, 2).Range.Text = card.Document.Receipt_Date.ToShortDateString();
                    docTable.Cell(2, 3).Range.Text = card.Document.Source ?? "";
                    docTable.Cell(2, 4).Range.Text = card.Document.Copies_Count.ToString();
                    docTable.Cell(2, 5).Range.Text = card.Document.Storage_Type ?? "";
                    docTable.Cell(2, 6).Range.Text = card.Document.Shelf_Number ?? "-";
                    SetTableCellStyle(docTable, 2, docHeaders.Length);

                    AddEmptyParagraph(doc, 1);
                }
            }
        }

        private static void SetTableCellStyle(Word.Table table, int row, int colCount)
        {
            for (int col = 1; col <= colCount; col++)
            {
                table.Cell(row, col).Range.Font.Name = "Times New Roman";
                table.Cell(row, col).Range.Font.Size = 10;
                table.Cell(row, col).Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                table.Cell(row, col).VerticalAlignment = WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                table.Cell(row, col).Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpaceSingle;
                table.Cell(row, col).Range.ParagraphFormat.SpaceAfter = 0;
                table.Cell(row, col).Range.ParagraphFormat.SpaceBefore = 0;
            }
        }

        private static Word.Table CreateStyledTable(Word.Document doc, string[] headers)
        {
            var para = doc.Paragraphs.Add();
            para.Range.ParagraphFormat.SpaceBefore = 0;
            para.Range.ParagraphFormat.SpaceAfter = 0;

            Word.Table table = doc.Tables.Add(para.Range, 1, headers.Length);

            table.PreferredWidthType = WdPreferredWidthType.wdPreferredWidthPercent;
            table.PreferredWidth = 100;
            table.AllowAutoFit = true;

            table.Borders.Enable = 1;
            table.Borders.InsideLineStyle = WdLineStyle.wdLineStyleSingle;
            table.Borders.OutsideLineStyle = WdLineStyle.wdLineStyleSingle;
            table.Borders.InsideLineWidth = WdLineWidth.wdLineWidth050pt;
            table.Borders.OutsideLineWidth = WdLineWidth.wdLineWidth075pt;

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = table.Cell(1, i + 1);
                cell.Range.Text = headers[i];
                cell.Range.Font.Name = "Times New Roman";
                cell.Range.Font.Size = 11;
                cell.Range.Font.Bold = 1;
                cell.Range.Font.Color = WdColor.wdColorBlack;
                cell.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                cell.VerticalAlignment = WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                cell.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpaceSingle;
                cell.Range.ParagraphFormat.SpaceAfter = 0;
                cell.Range.ParagraphFormat.SpaceBefore = 0;
                cell.Shading.BackgroundPatternColor = WdColor.wdColorGray10;
            }

            table.Rows.HeightRule = WdRowHeightRule.wdRowHeightAtLeast;
            table.Rows.Height = 16;

            return table;
        }

        private static void AddSectionTitle(Word.Document doc, string text)
        {
            var para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Name = "Times New Roman";
            para.Range.Font.Size = 12;
            para.Range.Font.Bold = 1;
            para.Range.Font.Color = WdColor.wdColorBlack;
            para.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphLeft;
            para.Range.ParagraphFormat.SpaceBefore = 6;
            para.Range.ParagraphFormat.SpaceAfter = 6;
            para.Range.InsertParagraphAfter();
        }

        private static void ApplyTextStyle(Word.Paragraph para, int fontSize = 14)
        {
            para.Range.Font.Name = "Times New Roman";
            para.Range.Font.Size = fontSize;
            para.Range.Font.Bold = 0;
            para.Range.Font.Color = WdColor.wdColorBlack;
            para.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphJustify;
            para.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
            para.Range.ParagraphFormat.LineSpacing = 1.5f * fontSize;
            para.Range.ParagraphFormat.SpaceAfter = 0;
            para.Range.ParagraphFormat.FirstLineIndent = 35.4f;
        }

        private static void AddCenteredParagraph(Word.Document doc, string text, int fontSize, bool bold, int spaceAfter = 12)
        {
            var para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Name = "Times New Roman";
            para.Range.Font.Size = fontSize;
            para.Range.Font.Bold = bold ? 1 : 0;
            para.Range.Font.Color = WdColor.wdColorBlack;
            para.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
            para.Range.ParagraphFormat.LineSpacingRule = WdLineSpacing.wdLineSpace1pt5;
            para.Range.ParagraphFormat.SpaceAfter = spaceAfter;
            para.Range.InsertParagraphAfter();
        }

        private static void AddEmptyParagraph(Word.Document doc, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                var p = doc.Paragraphs.Add();
                p.Range.InsertParagraphAfter();
            }
        }

        private static void AddPageBreak(Word.Document doc)
        {
            var para = doc.Paragraphs.Add();
            para.Range.InsertBreak(WdBreakType.wdPageBreak);
        }

        private static List<Document> GetFilteredDocuments(ArchiveBaseEntities ctx, List<int> docIds, DateTime? s, DateTime? e)
        {
            var query = ctx.Document.AsQueryable();
            if (docIds != null && docIds.Any())
                query = query.Where(d => docIds.Contains(d.Id));
            else if (docIds != null && docIds.Count == 0)
                return new List<Document>();
            if (s.HasValue && e.HasValue)
                query = query.Where(d => d.Receipt_Date >= s.Value && d.Receipt_Date <= e.Value);
            return query.OrderBy(d => d.Receipt_Date).ToList();
        }

        private static List<Request> GetFilteredRequests(ArchiveBaseEntities ctx, List<int> docIds, DateTime? s, DateTime? e)
        {
            var query = ctx.Request.Include("User").Include("Document").AsQueryable();
            if (docIds != null && docIds.Any())
                query = query.Where(r => docIds.Contains(r.Document_Id));
            if (s.HasValue && e.HasValue)
                query = query.Where(r => r.Request_Date >= s.Value && r.Request_Date <= e.Value);
            return query.OrderBy(r => r.Request_Date).ToList();
        }

        private static List<Registration_Card> GetFilteredRegCards(ArchiveBaseEntities ctx, List<int> docIds, DateTime? s, DateTime? e)
        {
            var query = ctx.Registration_Card.Include("User").Include("Document").AsQueryable();
            if (docIds != null && docIds.Any())
                query = query.Where(c => docIds.Contains(c.Document_Id));
            if (s.HasValue && e.HasValue)
                query = query.Where(c => c.Registration_Date >= s.Value && c.Registration_Date <= e.Value);
            return query.OrderBy(c => c.Registration_Date).ToList();
        }

        private static void OpenExportedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch { }
        }

        private static void ReleaseWordObjects(params object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                try
                {
                    if (System.Runtime.InteropServices.Marshal.IsComObject(obj))
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                }
                catch { }
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}