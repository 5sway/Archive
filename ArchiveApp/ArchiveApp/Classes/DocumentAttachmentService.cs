using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace ArchiveApp
{
    public class DocumentAttachment
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string FileType { get; set; }
    }

    /// <summary>
    /// Сервис для управления вложениями (сканами) документов.
    /// Сохраняет файлы в структурированную папку Attachments/Documents/{DocumentId}/.
    /// Поддерживает прикрепление, получение списка и открытие файлов.
    /// </summary>
    public static class DocumentAttachmentService
    {
        private static readonly string BaseAttachmentsPath;

        static DocumentAttachmentService()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (baseDir.Contains(@"\bin\Debug") || baseDir.Contains(@"\bin\Release"))
            {
                var projectRoot = Directory.GetParent(baseDir).Parent.Parent.Parent;
                BaseAttachmentsPath = Path.Combine(projectRoot.FullName, "Attachments", "Documents");
            }
            else
            {
                BaseAttachmentsPath = Path.Combine(baseDir, "Attachments", "Documents");
            }

            try
            {
                Directory.CreateDirectory(BaseAttachmentsPath);
                System.Diagnostics.Debug.WriteLine("Сканы сохраняются в: " + BaseAttachmentsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось создать папку Attachments:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string AttachFile(int documentId)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                Title = "Выберите скан документа"
            };

            if (openFileDialog.ShowDialog() != true) return null;

            string targetDir = Path.Combine(BaseAttachmentsPath, documentId.ToString());
            Directory.CreateDirectory(targetDir);

            string extension = Path.GetExtension(openFileDialog.FileName);
            string newFileName = $"Doc_{documentId}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            string targetPath = Path.Combine(targetDir, newFileName);

            try
            {
                File.Copy(openFileDialog.FileName, targetPath, true);

                AuditService.Log("Прикреплён скан", "Document",
                    $"Документ ID: {documentId}, Файл: {newFileName}");

                MessageBox.Show($"Скан успешно сохранён в папку проекта!\n\n" +
                              $"Файл: {newFileName}\n" +
                              $"Путь: {targetPath}",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                return targetPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public static List<DocumentAttachment> GetAttachments(int documentId)
        {
            var attachments = new List<DocumentAttachment>();
            string dir = Path.Combine(BaseAttachmentsPath, documentId.ToString());

            if (!Directory.Exists(dir)) return attachments;

            foreach (var file in Directory.GetFiles(dir))
            {
                attachments.Add(new DocumentAttachment
                {
                    DocumentId = documentId,
                    FileName = Path.GetFileName(file),
                    FilePath = file,
                    UploadDate = File.GetCreationTime(file),
                    FileType = Path.GetExtension(file).ToUpper()
                });
            }

            return attachments.OrderByDescending(a => a.UploadDate).ToList();
        }

        public static void OpenAttachment(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}