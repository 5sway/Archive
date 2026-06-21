using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ArchiveApp
{
    /// <summary>
    /// Статический класс для хранения ссылки на основной Frame приложения.
    /// Используется для навигации между страницами из любого места программы.
    /// </summary>
    class Manager
    {
        public static Frame MainFrame { get; set; }
    }
}