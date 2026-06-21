using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Windows;

namespace ArchiveApp
{
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string UserLogin { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// Сервис ведения аудита действий пользователей.
    /// Хранит логи текущей сессии в памяти и сохраняет полную историю в JSON-файл.
    /// Ограничивает размер истории до 5000 записей для предотвращения разрастания файла.
    /// </summary>
    public static class AuditService
    {
        private static readonly List<AuditEntry> _currentSessionLogs = new List<AuditEntry>();
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AuditLogs");
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "full_audit.json");

        static AuditService()
        {
            Directory.CreateDirectory(LogDirectory);
        }

        public static void Log(string action, string entityType, string details = "")
        {
            var entry = new AuditEntry
            {
                UserLogin = UserData.CurrentUserName ?? UserData.CurrentUserRole ?? "Система",
                Action = action,
                EntityType = entityType,
                Details = details
            };

            _currentSessionLogs.Add(entry);
            SaveToFile(entry);
        }

        private static void SaveToFile(AuditEntry entry)
        {
            try
            {
                List<AuditEntry> allLogs = new List<AuditEntry>();

                if (File.Exists(LogFilePath))
                {
                    string json = File.ReadAllText(LogFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        allLogs = JsonConvert.DeserializeObject<List<AuditEntry>>(json) ?? new List<AuditEntry>();
                    }
                }

                allLogs.Add(entry);

                if (allLogs.Count > 5000)
                    allLogs = allLogs.Skip(allLogs.Count - 5000).ToList();

                string updatedJson = JsonConvert.SerializeObject(allLogs, Formatting.Indented);
                File.WriteAllText(LogFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения аудита: {ex.Message}");
            }
        }

        public static List<AuditEntry> GetCurrentSessionLogs() => _currentSessionLogs.ToList();

        public static List<AuditEntry> GetFullHistory()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    string json = File.ReadAllText(LogFilePath);
                    return JsonConvert.DeserializeObject<List<AuditEntry>>(json) ?? new List<AuditEntry>();
                }
            }
            catch { }
            return new List<AuditEntry>();
        }

        public static void ClearCurrentSession() => _currentSessionLogs.Clear();

        public static void ClearFullHistory()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
                _currentSessionLogs.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка очистки истории аудита: {ex.Message}");
                MessageBox.Show($"Не удалось очистить историю аудита: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static int GetHistoryCount()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    string json = File.ReadAllText(LogFilePath);
                    var logs = JsonConvert.DeserializeObject<List<AuditEntry>>(json);
                    return logs?.Count ?? 0;
                }
            }
            catch { }
            return 0;
        }

        public static int GetSessionCount() => _currentSessionLogs.Count;
    }
}